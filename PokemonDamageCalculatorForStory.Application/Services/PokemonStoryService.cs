using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using PokemonDamageCalculatorForStory.Application.Identity;
using PokemonDamageCalculatorForStory.Domain.Models;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.Services;

/// <summary>ストーリー攻略用ダメージ計算 API の主要ユースケースを提供します。</summary>
public sealed class PokemonStoryService
{
    private static readonly JsonSerializerOptions SnapshotSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IImportJobRepository _importJobRepository;
    private readonly StoryProgressionProjector _progressionProjector;
    private readonly IRulesetRepository _rulesetRepository;
    private readonly IRunRepository _runRepository;
    private readonly IShareRepository _shareRepository;

    /// <summary>サービスを初期化します。</summary>
    public PokemonStoryService(
        ICurrentUserAccessor currentUserAccessor,
        IRulesetRepository rulesetRepository,
        IRunRepository runRepository,
        IShareRepository shareRepository,
        IImportJobRepository importJobRepository,
        StoryProgressionProjector progressionProjector)
    {
        _currentUserAccessor = currentUserAccessor;
        _rulesetRepository = rulesetRepository;
        _runRepository = runRepository;
        _shareRepository = shareRepository;
        _importJobRepository = importJobRepository;
        _progressionProjector = progressionProjector;
    }

    /// <summary>ルールセット一覧を取得します。</summary>
    public Task<IReadOnlyList<Ruleset>> ListRulesetsAsync(CancellationToken cancellationToken)
        => _rulesetRepository.ListRulesetsAsync(cancellationToken);

    /// <summary>ルールセット詳細を取得します。</summary>
    public Task<Ruleset?> FindRulesetAsync(Guid rulesetId, CancellationToken cancellationToken)
        => _rulesetRepository.FindRulesetAsync(rulesetId, cancellationToken);

    /// <summary>version set 詳細を取得します。</summary>
    public Task<MasterVersionSet?> FindMasterVersionSetAsync(Guid versionSetId, CancellationToken cancellationToken)
        => _rulesetRepository.FindMasterVersionSetAsync(versionSetId, cancellationToken);

    /// <summary>version set 一覧を取得します。</summary>
    public Task<IReadOnlyList<MasterVersionSet>> ListMasterVersionSetsAsync(CancellationToken cancellationToken)
        => _rulesetRepository.ListMasterVersionSetsAsync(cancellationToken);

    /// <summary>現在ユーザーの run 一覧を取得します。</summary>
    public async Task<IReadOnlyList<RunAggregate>> ListRunsAsync(CancellationToken cancellationToken)
    {
        var user = RequireCurrentUser();
        return await _runRepository.ListRunsAsync(user.UserId, cancellationToken);
    }

    /// <summary>run 詳細を取得します。</summary>
    public async Task<RunAggregate> GetRunAsync(Guid runId, CancellationToken cancellationToken)
        => await RequireOwnedRunAsync(runId, cancellationToken);

    /// <summary>run を作成します。</summary>
    public async Task<RunAggregate> CreateRunAsync(Guid rulesetId, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Run 名は必須です。");
        }

        var user = RequireCurrentUser();
        var ruleset = await _rulesetRepository.FindRulesetAsync(rulesetId, cancellationToken)
            ?? throw new KeyNotFoundException("指定された ruleset が見つかりません。");
        var versionSet = (await _rulesetRepository.ListMasterVersionSetsAsync(cancellationToken))
            .Where(set => set.RulesetId == ruleset.Id && set.IsPublished)
            .OrderByDescending(set => set.ImportedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("公開済み master version set が存在しません。");

        var now = DateTimeOffset.UtcNow;
        var run = new RunAggregate(
            Guid.NewGuid(),
            user.UserId,
            ruleset.Id,
            versionSet.Id,
            name.Trim(),
            "draft",
            null,
            Array.Empty<RoutePlan>(),
            Array.Empty<EnemyGroupDefinition>(),
            Array.Empty<CalculationPreset>(),
            now,
            now);

        await _runRepository.SaveRunAsync(run, cancellationToken);
        return run;
    }

    /// <summary>run の名前と状態を更新します。</summary>
    public async Task<RunAggregate> UpdateRunAsync(Guid runId, string name, string status, CancellationToken cancellationToken)
    {
        var run = await RequireOwnedRunAsync(runId, cancellationToken);
        var updated = run with
        {
            Name = string.IsNullOrWhiteSpace(name) ? run.Name : name.Trim(),
            Status = string.IsNullOrWhiteSpace(status) ? run.Status : status.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updated, cancellationToken);
        return updated;
    }

    /// <summary>run の初期状態を取得します。</summary>
    public async Task<RunInitialState?> GetInitialStateAsync(Guid runId, CancellationToken cancellationToken)
        => (await RequireOwnedRunAsync(runId, cancellationToken)).InitialState;

    /// <summary>run の初期状態を更新します。</summary>
    public async Task<RunAggregate> UpsertInitialStateAsync(Guid runId, RunInitialState initialState, CancellationToken cancellationToken)
    {
        ValidateInitialState(initialState);

        var run = await RequireOwnedRunAsync(runId, cancellationToken);
        var nextInitialState = initialState with { Revision = (run.InitialState?.Revision ?? 0) + 1 };
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var updated = run with
        {
            InitialState = nextInitialState,
            Routes = run.Routes
                .Select(route => RecalculateFingerprint(route with { IsStale = true }, nextInitialState.Revision, versionSet.VersionCatalog))
                .ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updated, cancellationToken);
        return updated;
    }

    /// <summary>route を作成します。</summary>
    public async Task<RoutePlan> CreateRouteAsync(Guid runId, string name, IReadOnlyList<Guid>? simulatedPartyMemberIds, CancellationToken cancellationToken)
    {
        var run = await RequireOwnedRunAsync(runId, cancellationToken);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Route 名は必須です。");
        }

        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var route = RecalculateFingerprint(
            new RoutePlan(
                Guid.NewGuid(),
                runId,
                name.Trim(),
                false,
                run.InitialState?.Revision ?? 0,
                Array.Empty<int>(),
                "empty",
                Array.Empty<ProgressionEvent>(),
                Array.Empty<BattleDefinition>(),
                simulatedPartyMemberIds?.ToArray() ?? Array.Empty<Guid>(),
                null),
            run.InitialState?.Revision ?? 0,
            versionSet.VersionCatalog);

        var updated = run with
        {
            Routes = run.Routes.Append(route).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updated, cancellationToken);
        return route;
    }

    /// <summary>run 配下の計算プリセット一覧を取得します。</summary>
    public async Task<IReadOnlyList<CalculationPreset>> ListPresetsAsync(Guid runId, CancellationToken cancellationToken)
        => (await RequireOwnedRunAsync(runId, cancellationToken)).Presets.OrderBy(item => item.Id).ToArray();

    /// <summary>計算プリセットを取得します。</summary>
    public async Task<CalculationPreset> GetPresetAsync(Guid presetId, CancellationToken cancellationToken)
        => await RequireOwnedPresetAsync(presetId, cancellationToken);

    /// <summary>計算プリセットを作成します。</summary>
    public async Task<CalculationPreset> CreatePresetAsync(CalculationPreset preset, CancellationToken cancellationToken)
    {
        ValidatePreset(preset);
        var run = await RequireOwnedRunAsync(preset.RunId, cancellationToken);
        var created = preset with { Id = Guid.NewGuid(), Revision = 1 };
        var updated = run with
        {
            Presets = run.Presets.Append(created).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updated, cancellationToken);
        return created;
    }

    /// <summary>計算プリセットを更新します。</summary>
    public async Task<CalculationPreset> UpdatePresetAsync(Guid presetId, CalculationPreset preset, CancellationToken cancellationToken)
    {
        ValidatePreset(preset);
        var (run, existingPreset) = await RequireOwnedPresetContainerAsync(presetId, cancellationToken);
        if (preset.RunId != run.Id)
        {
            throw new ArgumentException("preset は同じ run 配下で更新してください。");
        }

        var updatedPreset = preset with { Id = presetId, Revision = existingPreset.Revision + 1 };
        var updatedRun = run with
        {
            Presets = run.Presets.Select(item => item.Id == presetId ? updatedPreset : item).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updatedRun, cancellationToken);
        return updatedPreset;
    }

    /// <summary>event を追加します。</summary>
    public async Task<RoutePlan> AddProgressionEventAsync(Guid routeId, ProgressionEvent progressionEvent, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        ValidateProgressionEvent(run, route, progressionEvent);

        var nextSequence = route.Events.Count + 1;
        var eventToAdd = progressionEvent with
        {
            Id = progressionEvent.Id == Guid.Empty ? Guid.NewGuid() : progressionEvent.Id,
            Sequence = nextSequence,
            Revision = progressionEvent.Revision <= 0 ? 1 : progressionEvent.Revision
        };

        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var updatedRoute = RecalculateFingerprint(route with
        {
            Events = route.Events.Append(eventToAdd).OrderBy(item => item.Sequence).ToArray(),
            IsStale = true
        }, run.InitialState?.Revision ?? 0, versionSet.VersionCatalog);

        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);
        return updatedRoute;
    }

    /// <summary>event を更新します。</summary>
    public async Task<RoutePlan> UpdateProgressionEventAsync(Guid routeId, Guid eventId, ProgressionEvent progressionEvent, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        ValidateProgressionEvent(run, route, progressionEvent);

        var existing = route.Events.SingleOrDefault(item => item.Id == eventId)
            ?? throw new KeyNotFoundException("指定された progression event が見つかりません。");
        var updatedEvents = route.Events
            .Select(item => item.Id == eventId
                ? progressionEvent with
                {
                    Id = existing.Id,
                    Sequence = existing.Sequence,
                    Revision = existing.Revision + 1
                }
                : item)
            .OrderBy(item => item.Sequence)
            .ToArray();

        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var updatedRoute = RecalculateFingerprint(route with
        {
            Events = updatedEvents,
            IsStale = true
        }, run.InitialState?.Revision ?? 0, versionSet.VersionCatalog);

        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);
        return updatedRoute;
    }

    /// <summary>route 内 event を並び替えます。</summary>
    public async Task<RoutePlan> ReorderRouteAsync(Guid routeId, IReadOnlyList<Guid> orderedEventIds, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        if (orderedEventIds.Count != route.Events.Count || orderedEventIds.Distinct().Count() != route.Events.Count)
        {
            throw new ArgumentException("並び替え対象 event が一致しません。");
        }

        var lookup = route.Events.ToDictionary(item => item.Id);
        var reordered = orderedEventIds
            .Select((eventId, index) =>
            {
                var item = lookup.TryGetValue(eventId, out var existing)
                    ? existing
                    : throw new KeyNotFoundException("指定された progression event が route に存在しません。");
                return item with { Sequence = index + 1, Revision = item.Revision + 1 };
            })
            .ToArray();

        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var updatedRoute = RecalculateFingerprint(route with
        {
            Events = reordered,
            IsStale = true
        }, run.InitialState?.Revision ?? 0, versionSet.VersionCatalog);

        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);
        return updatedRoute;
    }

    /// <summary>enemy group を追加します。</summary>
    public async Task<EnemyGroupDefinition> AddEnemyGroupAsync(Guid runId, EnemyGroupDefinition group, CancellationToken cancellationToken)
    {
        var run = await RequireOwnedRunAsync(runId, cancellationToken);
        var created = group with
        {
            Id = group.Id == Guid.Empty ? Guid.NewGuid() : group.Id,
            RunId = runId
        };

        var updated = run with
        {
            EnemyGroups = run.EnemyGroups.Append(created).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updated, cancellationToken);
        return created;
    }

    /// <summary>enemy group を更新します。</summary>
    public async Task<EnemyGroupDefinition> UpdateEnemyGroupAsync(Guid runId, Guid groupId, EnemyGroupDefinition group, CancellationToken cancellationToken)
    {
        var run = await RequireOwnedRunAsync(runId, cancellationToken);
        var updatedGroups = run.EnemyGroups
            .Select(item => item.Id == groupId ? group with { Id = groupId, RunId = runId } : item)
            .ToArray();

        if (updatedGroups.All(item => item.Id != groupId))
        {
            throw new KeyNotFoundException("指定された enemy group が見つかりません。");
        }

        var updatedRun = run with
        {
            EnemyGroups = updatedGroups,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updatedRun, cancellationToken);
        return updatedGroups.Single(item => item.Id == groupId);
    }

    /// <summary>battle を追加します。</summary>
    public async Task<BattleDefinition> AddBattleAsync(Guid routeId, BattleDefinition battle, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        ValidateBattleDefinition(run, battle);

        var created = battle with
        {
            Id = battle.Id == Guid.Empty ? Guid.NewGuid() : battle.Id,
            RouteId = routeId
        };

        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var updatedRoute = RecalculateFingerprint(route with
        {
            Battles = route.Battles.Append(created).ToArray(),
            IsStale = true
        }, run.InitialState?.Revision ?? 0, versionSet.VersionCatalog);

        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);
        return created;
    }

    /// <summary>battle を更新します。</summary>
    public async Task<BattleDefinition> UpdateBattleAsync(Guid routeId, Guid battleId, BattleDefinition battle, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        ValidateBattleDefinition(run, battle);

        var updatedBattles = route.Battles
            .Select(item => item.Id == battleId ? battle with { Id = battleId, RouteId = routeId } : item)
            .ToArray();

        if (updatedBattles.All(item => item.Id != battleId))
        {
            throw new KeyNotFoundException("指定された battle が見つかりません。");
        }

        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var updatedRoute = RecalculateFingerprint(route with
        {
            Battles = updatedBattles,
            IsStale = true
        }, run.InitialState?.Revision ?? 0, versionSet.VersionCatalog);

        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);
        return updatedBattles.Single(item => item.Id == battleId);
    }

    /// <summary>battle ごとの参加計画を更新します。</summary>
    public async Task<BattleDefinition> UpdateBattleParticipationAsync(Guid battleId, IReadOnlyList<Guid> suggestedPartyMemberIds, IReadOnlyList<BattleParticipationPlan> participations, CancellationToken cancellationToken)
    {
        var (run, route, battle) = await RequireOwnedBattleAsync(battleId, cancellationToken);
        var updatedBattle = battle with
        {
            SuggestedPartyMemberIds = suggestedPartyMemberIds.ToArray(),
            Participations = participations.ToArray()
        };

        ValidateBattleDefinition(run, updatedBattle);
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var updatedRoute = RecalculateFingerprint(route with
        {
            Battles = route.Battles.Select(item => item.Id == battleId ? updatedBattle : item).ToArray(),
            IsStale = true
        }, run.InitialState?.Revision ?? 0, versionSet.VersionCatalog);

        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);
        return updatedBattle;
    }

    /// <summary>battle quick search を実行します。</summary>
    public async Task<IReadOnlyList<BattleSearchHit>> SearchBattlesAsync(Guid runId, string? keyword, CancellationToken cancellationToken)
    {
        var run = await RequireOwnedRunAsync(runId, cancellationToken);
        var normalizedKeyword = keyword?.Trim();

        return run.Routes
            .SelectMany(route => route.Battles.Select(battle => new { route, battle }))
            .Where(item =>
                string.IsNullOrWhiteSpace(normalizedKeyword)
                || item.battle.Title.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)
                || BuildEnemySummary(run, item.battle).Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase))
            .Select(item => new BattleSearchHit(
                item.battle.Id,
                item.route.Id,
                item.route.Name,
                item.battle.Title,
                item.battle.BattleKind,
                BuildEnemySummary(run, item.battle)))
            .ToArray();
    }

    /// <summary>route の進捗投影を取得します。</summary>
    public async Task<RouteProgressionProjection> GetRouteProgressionAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        return await ProjectRouteAsync(run, route, cancellationToken);
    }

    /// <summary>route を再計算して stale を解消します。</summary>
    public async Task<RouteProgressionProjection> RecalculateRouteAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        var projection = await ProjectRouteAsync(run, route, cancellationToken);

        var updatedRoute = route with
        {
            IsStale = false,
            LastVerifiedAt = projection.CalculatedAt
        };
        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);

        return projection;
    }

    /// <summary>route-wide verification を実行します。</summary>
    public async Task<RouteVerificationResult> VerifyRouteAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        var issues = new List<VerificationMessage>();
        var warnings = new List<VerificationMessage>();

        if (run.InitialState is null)
        {
            issues.Add(new VerificationMessage("initial-state.missing", "初期状態が未設定です。", null));
        }
        else if (run.InitialState.BaselineParty.Count is <= 0 or > 6)
        {
            issues.Add(new VerificationMessage("party.count.invalid", "初期手持ちは 1 体以上 6 体以下である必要があります。", null));
        }

        if (route.Battles.Count == 0)
        {
            warnings.Add(new VerificationMessage("battle.none", "route に battle が未設定です。", null));
        }

        var expectedSequence = 1;
        foreach (var progressionEvent in route.Events.OrderBy(item => item.Sequence))
        {
            if (progressionEvent.Sequence != expectedSequence)
            {
                warnings.Add(new VerificationMessage("event.sequence-gap", $"event sequence {progressionEvent.Sequence} が不連続です。", progressionEvent.LinkedBattleId));
            }

            expectedSequence++;
        }

        foreach (var battle in route.Battles)
        {
            if (battle.EnemyGroupId.HasValue && run.EnemyGroups.All(group => group.Id != battle.EnemyGroupId.Value))
            {
                issues.Add(new VerificationMessage("enemy-group.missing", $"battle '{battle.Title}' が存在しない enemy group を参照しています。", battle.Id));
            }

            if (battle.SourceKind.Equals("arbitrary", StringComparison.OrdinalIgnoreCase) && battle.InlineEnemies.Count == 0)
            {
                issues.Add(new VerificationMessage("battle.arbitrary-empty", $"battle '{battle.Title}' に敵情報がありません。", battle.Id));
            }

            if (battle.IsOptional)
            {
                warnings.Add(new VerificationMessage("battle.optional", $"battle '{battle.Title}' は任意戦闘です。", battle.Id));
            }

            if (run.InitialState is not null)
            {
                foreach (var participation in battle.Participations)
                {
                    if (run.InitialState.BaselineParty.All(member => member.PartyMemberId != participation.PartyMemberId))
                    {
                        issues.Add(new VerificationMessage("battle.participation.member-missing", $"battle '{battle.Title}' が存在しない party member を参照しています。", battle.Id));
                    }
                }
            }
        }

        var projection = await ProjectRouteAsync(run, route, cancellationToken);
        warnings.AddRange(projection.Warnings);

        var ruleset = await _rulesetRepository.FindRulesetAsync(run.RulesetId, cancellationToken)
            ?? throw new KeyNotFoundException("ruleset が見つかりません。");
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");

        var result = new RouteVerificationResult(
            route.Id,
            DateTimeOffset.UtcNow,
            issues.Count == 0 ? "route verification passed" : "route verification found issues",
            issues.Distinct().ToArray(),
            warnings.Distinct().ToArray(),
            BuildSourceReferences(ruleset, versionSet, null));

        var updatedRoute = route with
        {
            IsStale = false,
            LastVerifiedAt = result.VerifiedAt
        };

        await SaveUpdatedRouteAsync(run, updatedRoute, cancellationToken);
        return result;
    }

    /// <summary>単一ケースダメージ計算を実行します。</summary>
    public async Task<DamageCalculationResult> CalculateDamageAsync(DamageCalculationRequest request, CancellationToken cancellationToken)
    {
        var run = await RequireOwnedRunAsync(request.RunId, cancellationToken);
        var route = run.Routes.SingleOrDefault(item => item.Id == request.RouteId)
            ?? throw new KeyNotFoundException("route が見つかりません。");
        var battle = route.Battles.SingleOrDefault(item => item.Id == request.BattleId)
            ?? throw new KeyNotFoundException("battle が見つかりません。");
        var ruleset = await _rulesetRepository.FindRulesetAsync(run.RulesetId, cancellationToken)
            ?? throw new KeyNotFoundException("ruleset が見つかりません。");
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var preset = ResolvePreset(run, request.PresetId);

        var progressionProjection = await ProjectRouteAsync(run, route, cancellationToken);
        var modifiers = new List<DamageModifier>();
        if (request.Attacker.PrimaryType == request.MoveType || request.Attacker.SecondaryType == request.MoveType)
        {
            modifiers.Add(new DamageModifier(
                "stab",
                "Same Type Attack Bonus",
                1.5m,
                new SourceReference(ruleset.Slug, $"{ruleset.Title} {ruleset.Version}", "ruleset")));
        }

        var effectiveness = request.TypeEffectivenessOverride
            ?? PokemonTypeChart.GetEffectiveness(request.MoveType, new PokemonTypeSlot(request.Defender.PrimaryType, request.Defender.SecondaryType));
        modifiers.Add(new DamageModifier(
            "effectiveness",
            "タイプ相性",
            effectiveness,
            new SourceReference(versionSet.VersionCatalog.TypeChartVersion, "type-chart", "master-version")));

        if (request.IsCritical)
        {
            modifiers.Add(new DamageModifier(
                "critical",
                "急所",
                1.5m,
                new SourceReference("battle.critical", battle.Title, "battle")));
        }

        modifiers.AddRange(request.AdditionalModifiers);

        var baseDamage = ((((2m * request.Attacker.Level) / 5m) + 2m) * request.MovePower * Math.Max(1, request.Attacker.Attack) / Math.Max(1, request.Defender.Defense) / 50m) + 2m;
        var totalModifier = modifiers.Aggregate(1m, (current, modifier) => current * modifier.Multiplier);
        var maxDamage = Math.Max(1, (int)Math.Floor(baseDamage * totalModifier));
        var minDamage = Math.Max(1, (int)Math.Floor(baseDamage * totalModifier * 0.85m));
        var warnings = _progressionProjector.GetPpWarnings(progressionProjection, request.PlayerPartyMemberId, request.MoveName);

        return new DamageCalculationResult(
            minDamage,
            maxDamage,
            modifiers,
            new[]
            {
                $"{request.Attacker.Species} が {request.MoveName} を使用しました。",
                $"battle '{battle.Title}' の敵 {request.Defender.Species} を前提に計算しました。"
            },
            new[]
            {
                $"baseDamage={decimal.Round(baseDamage, 2)}",
                $"typeEffectiveness={decimal.Round(effectiveness, 4)}",
                $"totalModifier={decimal.Round(totalModifier, 4)}",
                $"damageRange={minDamage}-{maxDamage}"
            },
            warnings,
            BuildSourceReferences(ruleset, versionSet, battle.Id, preset));
    }

    /// <summary>複数パターン比較を実行します。</summary>
    public async Task<IReadOnlyList<ComparisonPatternResult>> ComparePatternsAsync(
        DamageCalculationRequest baseRequest,
        IReadOnlyDictionary<string, (int MovePowerDelta, int AttackBonus, IReadOnlyList<DamageModifier> AdditionalModifiers)> patterns,
        CancellationToken cancellationToken)
    {
        var baseline = await CalculateDamageAsync(baseRequest, cancellationToken);
        var results = new List<ComparisonPatternResult>();

        foreach (var pattern in patterns)
        {
            var request = baseRequest with
            {
                MovePower = baseRequest.MovePower + pattern.Value.MovePowerDelta,
                Attacker = baseRequest.Attacker with { Attack = baseRequest.Attacker.Attack + pattern.Value.AttackBonus },
                AdditionalModifiers = baseRequest.AdditionalModifiers.Concat(pattern.Value.AdditionalModifiers).ToArray()
            };

            var result = await CalculateDamageAsync(request, cancellationToken);
            results.Add(new ComparisonPatternResult(
                pattern.Key,
                $"power {request.MovePower}, attack {request.Attacker.Attack}",
                result,
                new[]
                {
                    $"minimum damage delta: {result.MinimumDamage - baseline.MinimumDamage}",
                    $"maximum damage delta: {result.MaximumDamage - baseline.MaximumDamage}"
                }));
        }

        return results;
    }

    /// <summary>しきい値探索を実行します。</summary>
    public async Task<ThresholdSearchResult> ThresholdSearchAsync(ThresholdSearchRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ConditionMode, "allOf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("threshold-search は allOf 条件のみ対応しています。");
        }

        if (request.PriorityOrder.Count > 0)
        {
            throw new ArgumentException("threshold-search は priority 指定に対応していません。");
        }

        var normalizedStats = NormalizeSearchStats(request.SearchStats);
        if (normalizedStats.Count == 0)
        {
            throw new ArgumentException("threshold-search には探索対象 IV が必要です。");
        }

        var run = await RequireOwnedRunAsync(request.RunId, cancellationToken);
        var route = run.Routes.SingleOrDefault(item => item.Id == request.RouteId)
            ?? throw new KeyNotFoundException("route が見つかりません。");
        var battle = route.Battles.SingleOrDefault(item => item.Id == request.BattleId)
            ?? throw new KeyNotFoundException("battle が見つかりません。");
        var ruleset = await _rulesetRepository.FindRulesetAsync(run.RulesetId, cancellationToken)
            ?? throw new KeyNotFoundException("ruleset が見つかりません。");
        var defender = ResolveBattleEnemies(run, battle).FirstOrDefault()
            ?? throw new InvalidOperationException("battle に敵情報がありません。");
        var preset = ResolvePreset(run, request.PresetId);
        var attacker = _progressionProjector.ResolveProjectedPartyMember(run, route, ruleset, request.PlayerPartyMemberId);
        var solutions = new List<ThresholdSolution>();
        var unsatisfiedConditionKeys = new HashSet<string>(request.Conditions.Select(item => item.ConditionKey), StringComparer.Ordinal);
        foreach (var candidateIvs in EnumerateThresholdIvs(attacker.IndividualValues, normalizedStats, 0))
        {
            var candidateStats = BuildThresholdCombatStats(attacker, candidateIvs);
            var candidateAttacker = new CombatantSnapshot(
                attacker.Species,
                attacker.Level,
                candidateStats.Attack,
                candidateStats.Defense,
                attacker.Typing.PrimaryType,
                attacker.Typing.SecondaryType,
                attacker.HeldItem);
            var result = await CalculateDamageAsync(
                new DamageCalculationRequest(
                    run.Id,
                    route.Id,
                    battle.Id,
                    attacker.PartyMemberId,
                    request.PresetId,
                    request.MoveName,
                    request.MovePower,
                    request.MoveType,
                    candidateAttacker,
                    new CombatantSnapshot(defender.Species, defender.Level, defender.Attack, defender.Defense, defender.Typing.PrimaryType, defender.Typing.SecondaryType, null),
                    false,
                    null,
                    Array.Empty<DamageModifier>()),
                cancellationToken);

            var satisfied = request.Conditions
                .Where(condition => IsThresholdConditionSatisfied(condition, result, candidateStats))
                .Select(condition => condition.ConditionKey)
                .ToArray();

            if (satisfied.Length == request.Conditions.Count)
            {
                solutions.Add(new ThresholdSolution(candidateIvs, result.MinimumDamage, result.MaximumDamage, satisfied));
                foreach (var conditionKey in satisfied)
                {
                    unsatisfiedConditionKeys.Remove(conditionKey);
                }
            }
        }

        if (solutions.Count == 0)
        {
            return new ThresholdSearchResult(
                "no-solution",
                null,
                Array.Empty<ThresholdSolution>(),
                BuildThresholdSearchSummary(normalizedStats),
                unsatisfiedConditionKeys.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
                BuildThresholdSearchSources(battle, preset));
        }

        var minimalCost = solutions.Min(solution => GetThresholdSolutionCost(solution, normalizedStats));
        var minimalSolutions = solutions
            .Where(solution => GetThresholdSolutionCost(solution, normalizedStats) == minimalCost)
            .OrderBy(solution => GetThresholdSolutionSortKey(solution))
            .ToArray();
        var status = minimalSolutions.Length == 1 ? "solved" : "multiple-minimal-solutions";

        return new ThresholdSearchResult(
            status,
            minimalSolutions[0],
            minimalSolutions,
            BuildThresholdSearchSummary(normalizedStats),
            Array.Empty<string>(),
            BuildThresholdSearchSources(battle, preset));
    }

    /// <summary>share snapshot を新規公開します。</summary>
    public async Task<SharedRouteSnapshot> CreateShareAsync(Guid runId, string sourceType, Guid sourceId, string visibility, IReadOnlyList<string> allowedRoles, string summary, string? frozenInputJson, string? frozenOutputJson, CancellationToken cancellationToken)
    {
        var run = await RequireOwnedRunAsync(runId, cancellationToken);
        var (route, normalizedSourceType) = ResolveShareSource(run, sourceType, sourceId);
        var user = RequireCurrentUser();
        var normalizedVisibility = NormalizeVisibility(visibility);
        var normalizedRoles = NormalizeAllowedRoles(allowedRoles);
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var sharePolicyDigest = BuildSharePolicyDigest(normalizedVisibility, normalizedRoles);
        var projection = await ProjectRouteAsync(run, route, cancellationToken);
        var calculationInputDigest = BuildOptionalSnapshotDigest(frozenInputJson);
        var calculationOutputDigest = BuildOptionalSnapshotDigest(frozenOutputJson);
        var revision = await CreateRevisionAsync(
            run,
            route,
            user.UserId,
            string.IsNullOrWhiteSpace(summary) ? "initial publish" : summary.Trim(),
            null,
            normalizedSourceType,
            sourceId,
            versionSet.VersionCatalog.AdditionalVersionKeys,
            versionSet.VersionCatalog.ImportJobIds,
            sharePolicyDigest,
            route.ProgressionFingerprint,
            calculationInputDigest,
            calculationOutputDigest,
            frozenInputJson,
            frozenOutputJson,
            cancellationToken);
        var share = new SharedRouteSnapshot(
            Guid.NewGuid(),
            user.UserId,
            normalizedSourceType,
            sourceId,
            run.Id,
            route.Id,
            normalizedVisibility,
            normalizedRoles,
            revision.Id,
            revision.Id,
            versionSet.VersionCatalog,
            versionSet.VersionCatalog.AdditionalVersionKeys,
            versionSet.VersionCatalog.ImportJobIds,
            sharePolicyDigest,
            projection.ProgressionFingerprint,
            calculationInputDigest,
            calculationOutputDigest,
            new[] { revision },
            Array.Empty<ShareComment>(),
            DateTimeOffset.UtcNow);

        await _shareRepository.SaveShareAsync(share, cancellationToken);
        return share;
    }

    /// <summary>share を取得します。</summary>
    public async Task<SharedRouteSnapshot> GetShareAsync(Guid shareId, CancellationToken cancellationToken)
        => await RequireShareReadAccessAsync(shareId, cancellationToken);

    /// <summary>share comment 一覧を取得します。</summary>
    public async Task<IReadOnlyList<ShareComment>> ListCommentsAsync(Guid shareId, CancellationToken cancellationToken)
        => (await RequireShareReadAccessAsync(shareId, cancellationToken)).Comments;

    /// <summary>share comment を追加します。</summary>
    public async Task<SharedRouteSnapshot> AddCommentAsync(Guid shareId, Guid? revisionId, string body, CancellationToken cancellationToken)
    {
        var share = await RequireShareCapabilityAsync(shareId, "commenter", cancellationToken);
        var currentUser = RequireCurrentUser();

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("comment body は必須です。");
        }

        if (revisionId.HasValue && share.Revisions.All(item => item.Id != revisionId.Value))
        {
            throw new KeyNotFoundException("指定された revision が見つかりません。");
        }

        var comment = new ShareComment(Guid.NewGuid(), share.Id, revisionId, currentUser.UserId, body.Trim(), DateTimeOffset.UtcNow);
        var updated = share with { Comments = share.Comments.Append(comment).ToArray() };
        await _shareRepository.SaveShareAsync(updated, cancellationToken);
        return updated;
    }

    /// <summary>share revision 一覧を取得します。</summary>
    public async Task<IReadOnlyList<RouteRevision>> ListRevisionsAsync(Guid shareId, CancellationToken cancellationToken)
        => (await RequireShareReadAccessAsync(shareId, cancellationToken)).Revisions.OrderBy(item => item.CreatedAt).ToArray();

    /// <summary>share の新 revision を公開します。</summary>
    public async Task<SharedRouteSnapshot> PublishRevisionAsync(Guid shareId, string summary, CancellationToken cancellationToken)
    {
        var share = await RequireShareCapabilityAsync(shareId, "reviser", cancellationToken);
        if (!share.RunId.HasValue || !share.RouteId.HasValue)
        {
            throw new InvalidOperationException("share に紐づく route 情報が不足しています。");
        }

        var run = await _runRepository.FindRunAsync(share.RunId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("share に紐づく run が見つかりません。");
        var route = run.Routes.SingleOrDefault(item => item.Id == share.RouteId.Value)
            ?? throw new KeyNotFoundException("share に紐づく route が見つかりません。");
        var user = RequireCurrentUser();
        var revision = await CreateRevisionAsync(
            run,
            route,
            user.UserId,
            string.IsNullOrWhiteSpace(summary) ? "publish revision" : summary.Trim(),
            share.CurrentRevisionId,
            share.SourceType,
            share.SourceId,
            share.AdditionalMasterGroups,
            share.ImportJobIds,
            share.SharePolicyDigest,
            route.ProgressionFingerprint,
            share.CalculationInputDigest,
            share.CalculationOutputDigest,
            share.Revisions.Single(item => item.Id == share.CurrentRevisionId).SnapshotJson is { Length: > 0 } currentSnapshotJson
                ? JsonSerializer.Deserialize<RouteSnapshotDocument>(currentSnapshotJson, SnapshotSerializerOptions)?.FrozenInputJson
                : null,
            share.Revisions.Single(item => item.Id == share.CurrentRevisionId).SnapshotJson is { Length: > 0 } currentSnapshotJson2
                ? JsonSerializer.Deserialize<RouteSnapshotDocument>(currentSnapshotJson2, SnapshotSerializerOptions)?.FrozenOutputJson
                : null,
            cancellationToken);
        var updated = share with
        {
            ProgressionFingerprint = route.ProgressionFingerprint,
            CurrentRevisionId = revision.Id,
            Revisions = share.Revisions.Append(revision).ToArray()
        };

        await _shareRepository.SaveShareAsync(updated, cancellationToken);
        return updated;
    }

    /// <summary>revision diff を取得します。</summary>
    public async Task<ShareDiffResult> GetShareDiffAsync(Guid shareId, Guid? baseRevisionId, Guid? targetRevisionId, CancellationToken cancellationToken)
    {
        var share = await RequireShareReadAccessAsync(shareId, cancellationToken);
        var baseRevision = share.Revisions.SingleOrDefault(item => item.Id == (baseRevisionId ?? share.BaseRevisionId))
            ?? throw new KeyNotFoundException("base revision が見つかりません。");
        var targetRevision = share.Revisions.SingleOrDefault(item => item.Id == (targetRevisionId ?? share.CurrentRevisionId))
            ?? throw new KeyNotFoundException("target revision が見つかりません。");

        var baseSnapshot = JsonSerializer.Deserialize<RouteSnapshotDocument>(baseRevision.SnapshotJson, SnapshotSerializerOptions)
            ?? throw new InvalidOperationException("base snapshot を復元できません。");
        var targetSnapshot = JsonSerializer.Deserialize<RouteSnapshotDocument>(targetRevision.SnapshotJson, SnapshotSerializerOptions)
            ?? throw new InvalidOperationException("target snapshot を復元できません。");

        var changedFields = new List<string>();
        if (!string.Equals(baseSnapshot.SourceType, targetSnapshot.SourceType, StringComparison.Ordinal))
        {
            changedFields.Add("share.sourceType");
        }

        if (baseSnapshot.SourceId != targetSnapshot.SourceId)
        {
            changedFields.Add("share.sourceId");
        }

        if (!string.Equals(baseSnapshot.Route.Name, targetSnapshot.Route.Name, StringComparison.Ordinal))
        {
            changedFields.Add("route.name");
        }

        if (baseSnapshot.Route.InitialStateRevision != targetSnapshot.Route.InitialStateRevision)
        {
            changedFields.Add("route.initialStateRevision");
        }

        if (!baseSnapshot.Route.OrderedProgressionEventRevisions.SequenceEqual(targetSnapshot.Route.OrderedProgressionEventRevisions))
        {
            changedFields.Add("route.orderedProgressionEventRevision");
        }

        if (baseSnapshot.Route.Events.Count != targetSnapshot.Route.Events.Count)
        {
            changedFields.Add("route.events.count");
        }
        else if (!AreStructurallyEqual(baseSnapshot.Route.Events, targetSnapshot.Route.Events))
        {
            changedFields.Add("route.events.detail");
        }

        if (baseSnapshot.Route.Battles.Count != targetSnapshot.Route.Battles.Count)
        {
            changedFields.Add("route.battles.count");
        }
        else if (!AreStructurallyEqual(baseSnapshot.Route.Battles, targetSnapshot.Route.Battles))
        {
            changedFields.Add("route.battles.detail");
        }

        if ((baseSnapshot.InitialState?.Revision ?? 0) != (targetSnapshot.InitialState?.Revision ?? 0))
        {
            changedFields.Add("run.initial-state.revision");
        }

        if ((baseSnapshot.InitialState?.BaselineMoney ?? 0) != (targetSnapshot.InitialState?.BaselineMoney ?? 0))
        {
            changedFields.Add("run.initial-state.baselineMoney");
        }

        if (!AreStructurallyEqual(baseSnapshot.InitialState?.BaselineParty ?? Array.Empty<PartyMemberDefinition>(), targetSnapshot.InitialState?.BaselineParty ?? Array.Empty<PartyMemberDefinition>()))
        {
            changedFields.Add("run.initial-state.party");
        }

        if (!string.Equals(baseSnapshot.ProgressionFingerprint, targetSnapshot.ProgressionFingerprint, StringComparison.Ordinal))
        {
            changedFields.Add("progressionFingerprint");
        }

        if (!string.Equals(baseSnapshot.CalculationInputDigest, targetSnapshot.CalculationInputDigest, StringComparison.Ordinal))
        {
            changedFields.Add("calculationInputDigest");
        }

        if (!string.Equals(baseSnapshot.CalculationOutputDigest, targetSnapshot.CalculationOutputDigest, StringComparison.Ordinal))
        {
            changedFields.Add("calculationOutputDigest");
        }

        if (!AreStructurallyEqual(baseSnapshot.ProgressionProjection?.PartyMembers ?? Array.Empty<PartyMemberProjection>(), targetSnapshot.ProgressionProjection?.PartyMembers ?? Array.Empty<PartyMemberProjection>()))
        {
            changedFields.Add("progression.partyMembers");
        }

        if (!AreStructurallyEqual(baseSnapshot.ProgressionProjection?.Warnings ?? Array.Empty<VerificationMessage>(), targetSnapshot.ProgressionProjection?.Warnings ?? Array.Empty<VerificationMessage>()))
        {
            changedFields.Add("progression.warnings");
        }

        var changedVersions = CompareVersionCatalog(baseSnapshot.VersionCatalog, targetSnapshot.VersionCatalog);
        if (!baseSnapshot.ImportJobIds.OrderBy(item => item).SequenceEqual(targetSnapshot.ImportJobIds.OrderBy(item => item)))
        {
            changedVersions.Add("importJobIds");
        }

        if (!baseSnapshot.AdditionalMasterGroups.OrderBy(item => item, StringComparer.Ordinal).SequenceEqual(targetSnapshot.AdditionalMasterGroups.OrderBy(item => item, StringComparer.Ordinal)))
        {
            changedVersions.Add("additionalMasterGroups");
        }

        var changedPolicies = new List<string>();
        if (!string.Equals(baseSnapshot.SharePolicyDigest, targetSnapshot.SharePolicyDigest, StringComparison.Ordinal))
        {
            changedPolicies.Add("sharePolicyDigest");
        }

        return new ShareDiffResult(
            share.Id,
            baseRevision.Id,
            targetRevision.Id,
            changedFields.Count == 0 && changedVersions.Count == 0 && changedPolicies.Count == 0 ? "no structural diff" : $"{changedFields.Count + changedVersions.Count + changedPolicies.Count} field(s) changed",
            changedFields,
            changedVersions,
            changedPolicies);
    }

    /// <summary>dry-run import job を作成します。</summary>
    public Task<ImportJob> CreateDryRunImportJobAsync(Guid rulesetId, string workbookName, string sourceType, string? workbookContent, CancellationToken cancellationToken)
        => CreateImportJobCoreAsync(rulesetId, workbookName, "dry-run", sourceType, workbookContent, false, cancellationToken);

    /// <summary>commit import job を作成します。</summary>
    public Task<ImportJob> CreateCommitImportJobAsync(Guid rulesetId, string workbookName, string sourceType, string? workbookContent, CancellationToken cancellationToken)
        => CreateImportJobCoreAsync(rulesetId, workbookName, "commit", sourceType, workbookContent, true, cancellationToken);

    /// <summary>mode 指定で import job を作成します。</summary>
    public Task<ImportJob> CreateImportJobAsync(Guid rulesetId, string workbookName, string? mode, string? sourceType, string? workbookContent, CancellationToken cancellationToken)
    {
        var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "commit" : mode.Trim().ToLowerInvariant();
        var normalizedSourceType = string.IsNullOrWhiteSpace(sourceType) ? "spreadsheet" : sourceType.Trim().ToLowerInvariant();
        return normalizedMode switch
        {
            "dry-run" => CreateDryRunImportJobAsync(rulesetId, workbookName, normalizedSourceType, workbookContent, cancellationToken),
            "commit" => CreateCommitImportJobAsync(rulesetId, workbookName, normalizedSourceType, workbookContent, cancellationToken),
            _ => throw new ArgumentException("mode は dry-run / commit のいずれかで指定してください。")
        };
    }

    /// <summary>import job を取得します。</summary>
    public async Task<ImportJob> GetImportJobAsync(Guid jobId, CancellationToken cancellationToken)
        => await _importJobRepository.FindJobAsync(jobId, cancellationToken)
            ?? throw new KeyNotFoundException("import job が見つかりません。");

    /// <summary>master version set を公開状態へ更新します。</summary>
    public async Task<MasterVersionSet> PublishMasterVersionSetAsync(Guid versionSetId, CancellationToken cancellationToken)
    {
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(versionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var published = versionSet with { IsPublished = true };
        await _rulesetRepository.SaveMasterVersionSetAsync(published, cancellationToken);
        return published;
    }

    private async Task<ImportJob> CreateImportJobCoreAsync(Guid rulesetId, string workbookName, string mode, string sourceType, string? workbookContent, bool publishVersionSet, CancellationToken cancellationToken)
    {
        var user = RequireCurrentUser();
        var ruleset = await _rulesetRepository.FindRulesetAsync(rulesetId, cancellationToken)
            ?? throw new KeyNotFoundException("指定された ruleset が見つかりません。");
        var jobId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var parsedWorkbook = ParseWorkbookContent(workbookName, workbookContent);
        var rowResults = parsedWorkbook.RowResults;
        var duplicateSummary = parsedWorkbook.DuplicateSummary;
        var auditTrail = new[]
        {
            new ImportAuditEntry(mode, user.UserId, createdAt, $"Workbook '{workbookName.Trim()}' processed."),
            new ImportAuditEntry("row-validated", user.UserId, createdAt, $"{parsedWorkbook.RowResults.Count} row(s) parsed from {sourceType}."),
            new ImportAuditEntry(publishVersionSet ? "version-set-created" : "dry-run-completed", user.UserId, createdAt, ruleset.Slug)
        };

        Guid? publishedVersionSetId = null;
        if (publishVersionSet)
        {
            var versionSet = new MasterVersionSet(
                Guid.NewGuid(),
                rulesetId,
                $"{workbookName.Trim()} import {DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                DateTimeOffset.UtcNow,
                false,
                new VersionCatalog(
                    $"{ruleset.Slug}:damage:{ruleset.Version}",
                    $"{ruleset.Slug}:experience:{ruleset.Version}",
                    "pokemon-master-foundation",
                    "move-master-foundation",
                    "ability-master-foundation",
                    "item-master-foundation",
                    "type-chart-foundation",
                    "nature-master-foundation",
                    "story-enemy-master-foundation",
                    "experience-table-foundation",
                    "effort-value-master-foundation",
                    "pp-rule-foundation",
                    parsedWorkbook.AdditionalVersionKeys,
                    new[] { jobId }),
                new[]
                {
                    new SourceReference($"{sourceType}.sheet1", workbookName.Trim(), sourceType),
                    new SourceReference("import.mode", mode, "import")
                });

            publishedVersionSetId = versionSet.Id;
            await _rulesetRepository.SaveMasterVersionSetAsync(versionSet, cancellationToken);
        }

        var job = new ImportJob(
            jobId,
            rulesetId,
            workbookName.Trim(),
            mode,
            sourceType,
            "completed",
            user.UserId,
            $"workbook:{workbookName.Trim()};sourceType:{sourceType};rowCount:{parsedWorkbook.RowResults.Count}",
            publishVersionSet ? "import committed and version set prepared" : "dry-run completed",
            parsedWorkbook.Messages,
            rowResults,
            duplicateSummary,
            auditTrail,
            publishedVersionSetId,
            createdAt,
            createdAt);

        await _importJobRepository.SaveJobAsync(job, cancellationToken);
        return job;
    }

    private async Task<RouteProgressionProjection> ProjectRouteAsync(RunAggregate run, RoutePlan route, CancellationToken cancellationToken)
    {
        var ruleset = await _rulesetRepository.FindRulesetAsync(run.RulesetId, cancellationToken)
            ?? throw new KeyNotFoundException("ruleset が見つかりません。");
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        return _progressionProjector.Project(run, route, ruleset, versionSet);
    }

    private async Task<RunAggregate> RequireOwnedRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var user = RequireCurrentUser();
        var run = await _runRepository.FindRunAsync(runId, cancellationToken)
            ?? throw new KeyNotFoundException("run が見つかりません。");
        if (!string.Equals(run.OwnerUserId, user.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("この run へアクセスする権限がありません。");
        }

        return run;
    }

    private async Task<(RunAggregate Run, RoutePlan Route)> RequireOwnedRouteAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var runs = await ListRunsAsync(cancellationToken);
        var match = runs
            .Select(run => new { run, route = run.Routes.SingleOrDefault(item => item.Id == routeId) })
            .SingleOrDefault(item => item.route is not null);

        if (match is null || match.route is null)
        {
            throw new KeyNotFoundException("route が見つかりません。");
        }

        return (match.run, match.route);
    }

    private async Task<(RunAggregate Run, RoutePlan Route, BattleDefinition Battle)> RequireOwnedBattleAsync(Guid battleId, CancellationToken cancellationToken)
    {
        var runs = await ListRunsAsync(cancellationToken);
        foreach (var run in runs)
        {
            foreach (var route in run.Routes)
            {
                var battle = route.Battles.SingleOrDefault(item => item.Id == battleId);
                if (battle is not null)
                {
                    return (run, route, battle);
                }
            }
        }

        throw new KeyNotFoundException("battle が見つかりません。");
    }

    private async Task<CalculationPreset> RequireOwnedPresetAsync(Guid presetId, CancellationToken cancellationToken)
        => (await RequireOwnedPresetContainerAsync(presetId, cancellationToken)).Preset;

    private async Task<(RunAggregate Run, CalculationPreset Preset)> RequireOwnedPresetContainerAsync(Guid presetId, CancellationToken cancellationToken)
    {
        var runs = await ListRunsAsync(cancellationToken);
        foreach (var run in runs)
        {
            var preset = run.Presets.SingleOrDefault(item => item.Id == presetId);
            if (preset is not null)
            {
                return (run, preset);
            }
        }

        throw new KeyNotFoundException("preset が見つかりません。");
    }

    private async Task<SharedRouteSnapshot> RequireShareReadAccessAsync(Guid shareId, CancellationToken cancellationToken)
    {
        var share = await _shareRepository.FindShareAsync(shareId, cancellationToken)
            ?? throw new KeyNotFoundException("share が見つかりません。");
        var currentUser = RequireCurrentUser();
        if (string.Equals(currentUser.UserId, share.OwnerUserId, StringComparison.Ordinal))
        {
            return share;
        }

        if (HasShareReadCapability(share, currentUser))
        {
            return share;
        }

        throw new UnauthorizedAccessException("この share へのアクセス権がありません。");
    }

    private async Task<SharedRouteSnapshot> RequireShareCapabilityAsync(Guid shareId, string role, CancellationToken cancellationToken)
    {
        var share = await _shareRepository.FindShareAsync(shareId, cancellationToken)
            ?? throw new KeyNotFoundException("share が見つかりません。");
        var currentUser = RequireCurrentUser();
        if (string.Equals(currentUser.UserId, share.OwnerUserId, StringComparison.Ordinal))
        {
            return share;
        }

        if (HasShareRole(share, role) &&
            currentUser.Roles.Any(currentRole => string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase)) &&
            !string.Equals(share.Visibility, "private", StringComparison.OrdinalIgnoreCase))
        {
            return share;
        }

        throw new UnauthorizedAccessException("この share へのアクセス権がありません。");
    }

    private async Task<SharedRouteSnapshot> RequireShareOwnerAsync(Guid shareId, CancellationToken cancellationToken)
    {
        var share = await _shareRepository.FindShareAsync(shareId, cancellationToken)
            ?? throw new KeyNotFoundException("share が見つかりません。");
        var currentUser = RequireCurrentUser();
        if (!string.Equals(currentUser.UserId, share.OwnerUserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("この share を更新する権限がありません。");
        }

        return share;
    }

    private CurrentUser RequireCurrentUser()
        => _currentUserAccessor.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("認証が必要です。");

    private static string NormalizeVisibility(string visibility)
    {
        var normalized = string.IsNullOrWhiteSpace(visibility) ? "private" : visibility.Trim().ToLowerInvariant();
        return normalized switch
        {
            "private" => normalized,
            "unlisted" => normalized,
            "public" => normalized,
            _ => throw new ArgumentException("visibility は private / unlisted / public のいずれかで指定してください。")
        };
    }

    private static void ValidateInitialState(RunInitialState initialState)
    {
        if (initialState.BaselineParty.Count is <= 0 or > 6)
        {
            throw new ArgumentException("初期手持ちは 1 体以上 6 体以下で指定してください。");
        }

        if (initialState.BaselineParty.Select(member => member.Slot).Distinct().Count() != initialState.BaselineParty.Count)
        {
            throw new ArgumentException("手持ちスロット番号が重複しています。");
        }

        foreach (var member in initialState.BaselineParty)
        {
            if (string.IsNullOrWhiteSpace(member.Species) || member.Level <= 0 || member.Moves.Count == 0)
            {
                throw new ArgumentException("初期状態の必須項目が不足しています。");
            }
        }
    }

    private static void ValidatePreset(CalculationPreset preset)
    {
        if (preset.RunId == Guid.Empty)
        {
            throw new ArgumentException("preset の runId は必須です。");
        }

        if (preset.IvRanges.Count == 0)
        {
            throw new ArgumentException("preset には少なくとも 1 件の IV range が必要です。");
        }
    }

    private static void ValidateProgressionEvent(RunAggregate run, RoutePlan route, ProgressionEvent progressionEvent)
    {
        if (string.IsNullOrWhiteSpace(progressionEvent.EventType))
        {
            throw new ArgumentException("eventType は必須です。");
        }

        if (progressionEvent.LinkedBattleId.HasValue && route.Battles.All(item => item.Id != progressionEvent.LinkedBattleId.Value))
        {
            throw new ArgumentException("linkedBattleId が route に存在しません。");
        }

        if (run.InitialState is not null)
        {
            foreach (var partyDelta in progressionEvent.PartyDeltas)
            {
                if (run.InitialState.BaselineParty.All(member => member.PartyMemberId != partyDelta.PartyMemberId))
                {
                    throw new ArgumentException("event が存在しない party member を参照しています。");
                }
            }
        }
    }

    private static void ValidateBattleDefinition(RunAggregate run, BattleDefinition battle)
    {
        if (string.IsNullOrWhiteSpace(battle.Title))
        {
            throw new ArgumentException("battle title は必須です。");
        }

        if (battle.EnemyGroupId.HasValue && run.EnemyGroups.All(group => group.Id != battle.EnemyGroupId.Value))
        {
            throw new ArgumentException("参照先 enemy group が存在しません。");
        }

        if (battle.SourceKind.Equals("arbitrary", StringComparison.OrdinalIgnoreCase) && battle.InlineEnemies.Count == 0)
        {
            throw new ArgumentException("arbitrary battle には敵情報が必要です。");
        }

        if (run.InitialState is not null)
        {
            foreach (var partyMemberId in battle.SuggestedPartyMemberIds)
            {
                if (run.InitialState.BaselineParty.All(member => member.PartyMemberId != partyMemberId))
                {
                    throw new ArgumentException("suggested party member が存在しません。");
                }
            }

            foreach (var participation in battle.Participations)
            {
                if (run.InitialState.BaselineParty.All(member => member.PartyMemberId != participation.PartyMemberId))
                {
                    throw new ArgumentException("battle participation が存在しない party member を参照しています。");
                }
            }
        }
    }

    private static RoutePlan RecalculateFingerprint(RoutePlan route, int initialStateRevision, VersionCatalog versionCatalog)
    {
        var orderedEventRevisions = route.Events.OrderBy(item => item.Sequence).Select(item => item.Revision).ToArray();
        var fingerprintSource = string.Join(
            "|",
            new[] { $"initialStateRevision:{initialStateRevision}", $"versionDigest:{BuildVersionDigest(versionCatalog)}" }
            .Concat(route.Events.OrderBy(item => item.Sequence).Select(item => $"{item.Sequence}:{item.EventType}:{item.LinkedBattleId}:{item.Summary}:{item.Revision}"))
            .Concat(route.Battles.OrderBy(item => item.Title).Select(item =>
                $"{item.Title}:{item.SourceKind}:{item.BattleKind}:{item.IsOptional}:{item.EnemyGroupId}:{string.Join(",", item.Participations.Select(participation => $"{participation.PartyMemberId}:{participation.ParticipationMode}:{participation.ShareRatio}"))}"))
            .Concat(route.SimulatedPartyMemberIds.OrderBy(item => item).Select(item => item.ToString("N"))));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource));
        return route with
        {
            InitialStateRevision = initialStateRevision,
            OrderedProgressionEventRevisions = orderedEventRevisions,
            ProgressionFingerprint = Convert.ToHexString(bytes[..8])
        };
    }

    private async Task SaveUpdatedRouteAsync(RunAggregate run, RoutePlan updatedRoute, CancellationToken cancellationToken)
    {
        var updatedRun = run with
        {
            Routes = run.Routes.Select(route => route.Id == updatedRoute.Id ? updatedRoute : route).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updatedRun, cancellationToken);
    }

    private static IReadOnlyList<EnemyCombatant> ResolveBattleEnemies(RunAggregate run, BattleDefinition battle)
    {
        if (battle.EnemyGroupId.HasValue)
        {
            return run.EnemyGroups.SingleOrDefault(group => group.Id == battle.EnemyGroupId.Value)?.Members
                ?? Array.Empty<EnemyCombatant>();
        }

        return battle.InlineEnemies;
    }

    private static IReadOnlyList<string> NormalizeSearchStats(IReadOnlyList<string> searchStats)
        => searchStats
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item switch
            {
                "hp" => "hp",
                "attack" => "attack",
                "defense" => "defense",
                "specialAttack" => "specialAttack",
                "specialDefense" => "specialDefense",
                "speed" => "speed",
                _ => throw new ArgumentException($"threshold-search の探索対象に未対応の stat です: {item}")
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<StatValues> EnumerateThresholdIvs(StatValues baseline, IReadOnlyList<string> searchStats, int index)
    {
        if (index >= searchStats.Count)
        {
            yield return baseline;
            yield break;
        }

        var stat = searchStats[index];
        for (var value = 0; value <= 31; value++)
        {
            var next = stat switch
            {
                "hp" => baseline with { Hp = value },
                "attack" => baseline with { Attack = value },
                "defense" => baseline with { Defense = value },
                "specialAttack" => baseline with { SpecialAttack = value },
                "specialDefense" => baseline with { SpecialDefense = value },
                "speed" => baseline with { Speed = value },
                _ => baseline
            };

            foreach (var child in EnumerateThresholdIvs(next, searchStats, index + 1))
            {
                yield return child;
            }
        }
    }

    private static CombatStatSnapshot BuildThresholdCombatStats(PartyMemberDefinition attacker, StatValues candidateIvs)
    {
        var hpDelta = candidateIvs.Hp - attacker.IndividualValues.Hp;
        var attackDelta = candidateIvs.Attack - attacker.IndividualValues.Attack;
        var defenseDelta = candidateIvs.Defense - attacker.IndividualValues.Defense;
        var specialAttackDelta = candidateIvs.SpecialAttack - attacker.IndividualValues.SpecialAttack;
        var specialDefenseDelta = candidateIvs.SpecialDefense - attacker.IndividualValues.SpecialDefense;
        var speedDelta = candidateIvs.Speed - attacker.IndividualValues.Speed;
        return new CombatStatSnapshot(
            Math.Max(1, attacker.CombatStats.Hp + hpDelta),
            Math.Max(1, attacker.CombatStats.Attack + attackDelta),
            Math.Max(1, attacker.CombatStats.Defense + defenseDelta),
            Math.Max(1, attacker.CombatStats.SpecialAttack + specialAttackDelta),
            Math.Max(1, attacker.CombatStats.SpecialDefense + specialDefenseDelta),
            Math.Max(1, attacker.CombatStats.Speed + speedDelta));
    }

    private static bool IsThresholdConditionSatisfied(ThresholdCondition condition, DamageCalculationResult result, CombatStatSnapshot candidateStats)
        => condition.ConditionType switch
        {
            "minimum-damage-at-least" => result.MinimumDamage >= condition.ExpectedValue,
            "maximum-damage-at-most" => result.MaximumDamage <= condition.ExpectedValue,
            "hp-at-least" => candidateStats.Hp >= condition.ExpectedValue,
            "attack-at-least" => candidateStats.Attack >= condition.ExpectedValue,
            "defense-at-least" => candidateStats.Defense >= condition.ExpectedValue,
            "special-attack-at-least" => candidateStats.SpecialAttack >= condition.ExpectedValue,
            "special-defense-at-least" => candidateStats.SpecialDefense >= condition.ExpectedValue,
            "speed-at-least" or "action-order-at-least" => candidateStats.Speed >= condition.ExpectedValue,
            _ => false
        };

    private static int GetThresholdSolutionCost(ThresholdSolution solution, IReadOnlyList<string> searchStats)
        => searchStats.Sum(stat => stat switch
        {
            "hp" => solution.IndividualValues.Hp,
            "attack" => solution.IndividualValues.Attack,
            "defense" => solution.IndividualValues.Defense,
            "specialAttack" => solution.IndividualValues.SpecialAttack,
            "specialDefense" => solution.IndividualValues.SpecialDefense,
            "speed" => solution.IndividualValues.Speed,
            _ => 0
        });

    private static string GetThresholdSolutionSortKey(ThresholdSolution solution)
        => $"{solution.IndividualValues.Hp:D2}-{solution.IndividualValues.Attack:D2}-{solution.IndividualValues.Defense:D2}-{solution.IndividualValues.SpecialAttack:D2}-{solution.IndividualValues.SpecialDefense:D2}-{solution.IndividualValues.Speed:D2}";

    private static string BuildThresholdSearchSummary(IReadOnlyList<string> searchStats)
        => string.Join(", ", searchStats.Select(stat => $"{stat}:0..31 step1"));

    private static string BuildEnemySummary(RunAggregate run, BattleDefinition battle)
    {
        var enemies = ResolveBattleEnemies(run, battle);
        return enemies.Count == 0
            ? "enemy unresolved"
            : string.Join(", ", enemies.Select(item => $"{item.Species} Lv{item.Level}"));
    }

    private static (RoutePlan Route, string SourceType) ResolveShareSource(RunAggregate run, string sourceType, Guid sourceId)
    {
        var normalizedSourceType = string.IsNullOrWhiteSpace(sourceType) ? "route-plan" : sourceType.Trim().ToLowerInvariant();
        return normalizedSourceType switch
        {
            "route-plan" or "verification" => (
                run.Routes.SingleOrDefault(item => item.Id == sourceId)
                ?? throw new KeyNotFoundException("share 対象 route が見つかりません。"),
                normalizedSourceType),
            "calculation" or "comparison" => (
                run.Routes.SingleOrDefault(route => route.Battles.Any(battle => battle.Id == sourceId))
                ?? throw new KeyNotFoundException("share 対象 battle が見つかりません。"),
                normalizedSourceType),
            _ => throw new ArgumentException("sourceType は route-plan / verification / calculation / comparison のいずれかで指定してください。")
        };
    }

    private static IReadOnlyList<string> NormalizeAllowedRoles(IReadOnlyList<string> allowedRoles)
    {
        var roles = allowedRoles
            .Select(item => item.Trim().ToLowerInvariant())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item switch
            {
                "viewer" => "viewer",
                "commenter" => "commenter",
                "reviser" => "reviser",
                _ => throw new ArgumentException($"share role が未対応です: {item}")
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        return roles.Length == 0 ? new[] { "viewer" } : roles;
    }

    private static bool HasShareRole(SharedRouteSnapshot share, string role)
        => share.AllowedRoles.Contains(role, StringComparer.Ordinal);

    private static bool HasShareReadCapability(SharedRouteSnapshot share, CurrentUser currentUser)
        => !string.Equals(share.Visibility, "private", StringComparison.OrdinalIgnoreCase) &&
           currentUser.Roles.Any(currentRole =>
               (string.Equals(currentRole, "viewer", StringComparison.OrdinalIgnoreCase) && HasShareRole(share, "viewer")) ||
               (string.Equals(currentRole, "commenter", StringComparison.OrdinalIgnoreCase) && HasShareRole(share, "commenter")) ||
               (string.Equals(currentRole, "reviser", StringComparison.OrdinalIgnoreCase) && HasShareRole(share, "reviser")));

    private static bool AreStructurallyEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        => JsonSerializer.Serialize(left, SnapshotSerializerOptions) == JsonSerializer.Serialize(right, SnapshotSerializerOptions);

    private static string BuildSharePolicyDigest(string visibility, IReadOnlyList<string> allowedRoles)
    {
        var policySource = $"{visibility}|{string.Join(",", allowedRoles.OrderBy(item => item, StringComparer.Ordinal))}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(policySource)))[..16];
    }

    private async Task<RouteRevision> CreateRevisionAsync(
        RunAggregate run,
        RoutePlan route,
        string authorUserId,
        string summary,
        Guid? parentRevisionId,
        string sourceType,
        Guid sourceId,
        IReadOnlyList<string> additionalMasterGroups,
        IReadOnlyList<Guid> importJobIds,
        string sharePolicyDigest,
        string progressionFingerprint,
        string? calculationInputDigest,
        string? calculationOutputDigest,
        string? frozenInputJson,
        string? frozenOutputJson,
        CancellationToken cancellationToken)
    {
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var projection = await ProjectRouteAsync(run, route, cancellationToken);
        var snapshotDocument = new RouteSnapshotDocument(
            sourceType,
            sourceId,
            route,
            run.InitialState,
            run.EnemyGroups,
            NormalizeProjectionForSnapshot(projection),
            versionSet.VersionCatalog,
            additionalMasterGroups,
            importJobIds,
            sharePolicyDigest,
            progressionFingerprint,
            calculationInputDigest,
            calculationOutputDigest,
            frozenInputJson,
            frozenOutputJson);
        var snapshotJson = JsonSerializer.Serialize(snapshotDocument, SnapshotSerializerOptions);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));
        return new RouteRevision(Guid.NewGuid(), parentRevisionId, summary, snapshotJson, digest, authorUserId, DateTimeOffset.UtcNow);
    }

    private static string BuildSnapshotDigest(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];

    private static string? BuildOptionalSnapshotDigest(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : BuildSnapshotDigest(value);

    private static RouteProgressionProjection? NormalizeProjectionForSnapshot(RouteProgressionProjection? projection)
        => projection is null ? null : projection with { CalculatedAt = DateTimeOffset.UnixEpoch };

    private static (IReadOnlyList<ImportRowResult> RowResults, ImportDuplicateSummary DuplicateSummary, IReadOnlyList<string> Messages, IReadOnlyList<string> AdditionalVersionKeys) ParseWorkbookContent(string workbookName, string? workbookContent)
    {
        if (TryParseWorkbookArchive(workbookName, workbookContent, out var parsedWorkbook))
        {
            return parsedWorkbook;
        }

        var lines = (workbookContent ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
        {
            return (
                new[] { new ImportRowResult(2, "error", $"Workbook '{workbookName}' content is empty.") },
                new ImportDuplicateSummary(0, Array.Empty<int>()),
                new[] { "Workbook content was empty.", "No persistence side effects were applied." },
                new[] { "spreadsheet-empty-input" });
        }

        var rowResults = new List<ImportRowResult>();
        var duplicateRows = new List<int>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < lines.Length; index++)
        {
            var rowNumber = index + 2;
            var cells = lines[index].Split(',', StringSplitOptions.TrimEntries);
            if (cells.Length < 2)
            {
                rowResults.Add(new ImportRowResult(rowNumber, "error", "Expected at least 2 columns."));
                continue;
            }

            var key = cells[0];
            if (!seenKeys.Add(key))
            {
                duplicateRows.Add(rowNumber);
                rowResults.Add(new ImportRowResult(rowNumber, "duplicate", $"Duplicate key '{key}' detected."));
                continue;
            }

            rowResults.Add(new ImportRowResult(rowNumber, "success", $"Parsed '{key}' for ruleset import."));
        }

        var messages = new List<string>
        {
            $"{rowResults.Count(item => string.Equals(item.Status, "success", StringComparison.OrdinalIgnoreCase))} row(s) parsed.",
            $"{duplicateRows.Count} duplicate row(s) detected."
        };

        if (rowResults.Any(item => string.Equals(item.Status, "error", StringComparison.OrdinalIgnoreCase)))
        {
            messages.Add("Validation errors were detected.");
        }

        return (
            rowResults,
            new ImportDuplicateSummary(lines.Length, duplicateRows),
            messages,
            new[] { "evolution-rule-foundation", "participation-rule-foundation", "parsed-spreadsheet-content" });
    }

    private static bool TryParseWorkbookArchive(string workbookName, string? workbookContent, out (IReadOnlyList<ImportRowResult> RowResults, ImportDuplicateSummary DuplicateSummary, IReadOnlyList<string> Messages, IReadOnlyList<string> AdditionalVersionKeys) parsedWorkbook)
    {
        parsedWorkbook = default;
        if (string.IsNullOrWhiteSpace(workbookContent))
        {
            return false;
        }

        byte[] workbookBytes;
        try
        {
            workbookBytes = Convert.FromBase64String(workbookContent);
        }
        catch (FormatException)
        {
            return false;
        }

        if (workbookBytes.Length < 4 || workbookBytes[0] != 'P' || workbookBytes[1] != 'K')
        {
            return false;
        }

        using var memoryStream = new MemoryStream(workbookBytes, writable: false);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var rowResults = new List<ImportRowResult>();
        var duplicateRows = new List<int>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var worksheet in archive.Entries.Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)).OrderBy(entry => entry.FullName, StringComparer.Ordinal))
        {
            using var stream = worksheet.Open();
            var document = XDocument.Load(stream);
            var rows = document.Descendants().Where(element => element.Name.LocalName == "row");
            foreach (var row in rows)
            {
                var rowNumber = int.TryParse(row.Attribute("r")?.Value, out var parsedRowNumber) ? parsedRowNumber : rowResults.Count + 2;
                var values = row.Elements().Where(element => element.Name.LocalName == "c").Select(cell => ReadCellValue(cell, sharedStrings)).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
                if (values.Length == 0)
                {
                    continue;
                }

                var key = values[0];
                if (!seenKeys.Add(key))
                {
                    duplicateRows.Add(rowNumber);
                    rowResults.Add(new ImportRowResult(rowNumber, "duplicate", $"Duplicate key '{key}' detected in workbook."));
                    continue;
                }

                rowResults.Add(new ImportRowResult(rowNumber, "success", $"Parsed workbook row '{key}' with {values.Length} value(s)."));
            }
        }

        if (rowResults.Count == 0)
        {
            rowResults.Add(new ImportRowResult(2, "error", $"Workbook '{workbookName}' did not contain parsable worksheet rows."));
        }

        parsedWorkbook = (
            rowResults,
            new ImportDuplicateSummary(rowResults.Count, duplicateRows),
            new[]
            {
                $"Workbook archive '{workbookName}' parsed.",
                $"{duplicateRows.Count} duplicate row(s) detected."
            },
            new[] { "evolution-rule-foundation", "participation-rule-foundation", "parsed-workbook-archive" });
        return true;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedStringsEntry is null)
        {
            return Array.Empty<string>();
        }

        using var stream = sharedStringsEntry.Open();
        var document = XDocument.Load(stream);
        return document.Descendants()
            .Where(element => element.Name.LocalName == "si")
            .Select(element => string.Concat(element.Descendants().Where(node => node.Name.LocalName == "t").Select(node => node.Value)))
            .ToArray();
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var cellType = cell.Attribute("t")?.Value;
        if (string.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(cell.Descendants().Where(node => node.Name.LocalName == "t").Select(node => node.Value));
        }

        var value = cell.Elements().SingleOrDefault(element => element.Name.LocalName == "v")?.Value ?? string.Empty;
        if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var sharedStringIndex) && sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedStringIndex];
        }

        return value;
    }

    private static List<string> CompareVersionCatalog(VersionCatalog? baseCatalog, VersionCatalog? targetCatalog)
    {
        var changed = new List<string>();
        CompareVersionKey(changed, "damageRulesetVersion", baseCatalog?.DamageRulesetVersion, targetCatalog?.DamageRulesetVersion);
        CompareVersionKey(changed, "experienceRulesetVersion", baseCatalog?.ExperienceRulesetVersion, targetCatalog?.ExperienceRulesetVersion);
        CompareVersionKey(changed, "pokemonMasterVersion", baseCatalog?.PokemonMasterVersion, targetCatalog?.PokemonMasterVersion);
        CompareVersionKey(changed, "moveMasterVersion", baseCatalog?.MoveMasterVersion, targetCatalog?.MoveMasterVersion);
        CompareVersionKey(changed, "abilityMasterVersion", baseCatalog?.AbilityMasterVersion, targetCatalog?.AbilityMasterVersion);
        CompareVersionKey(changed, "itemMasterVersion", baseCatalog?.ItemMasterVersion, targetCatalog?.ItemMasterVersion);
        CompareVersionKey(changed, "typeChartVersion", baseCatalog?.TypeChartVersion, targetCatalog?.TypeChartVersion);
        CompareVersionKey(changed, "natureMasterVersion", baseCatalog?.NatureMasterVersion, targetCatalog?.NatureMasterVersion);
        CompareVersionKey(changed, "storyEnemyMasterVersion", baseCatalog?.StoryEnemyMasterVersion, targetCatalog?.StoryEnemyMasterVersion);
        CompareVersionKey(changed, "experienceTableVersion", baseCatalog?.ExperienceTableVersion, targetCatalog?.ExperienceTableVersion);
        CompareVersionKey(changed, "effortValueMasterVersion", baseCatalog?.EffortValueMasterVersion, targetCatalog?.EffortValueMasterVersion);
        CompareVersionKey(changed, "ppRuleVersion", baseCatalog?.PpRuleVersion, targetCatalog?.PpRuleVersion);
        return changed;
    }

    private static void CompareVersionKey(List<string> changed, string key, string? baseValue, string? targetValue)
    {
        if (!string.Equals(baseValue, targetValue, StringComparison.Ordinal))
        {
            changed.Add(key);
        }
    }

    private static string BuildVersionDigest(VersionCatalog versionCatalog)
    {
        var digestSource = string.Join(
            "|",
            new[]
            {
                versionCatalog.DamageRulesetVersion,
                versionCatalog.ExperienceRulesetVersion,
                versionCatalog.PokemonMasterVersion,
                versionCatalog.MoveMasterVersion,
                versionCatalog.AbilityMasterVersion,
                versionCatalog.ItemMasterVersion,
                versionCatalog.TypeChartVersion,
                versionCatalog.NatureMasterVersion,
                versionCatalog.StoryEnemyMasterVersion,
                versionCatalog.ExperienceTableVersion,
                versionCatalog.EffortValueMasterVersion,
                versionCatalog.PpRuleVersion
            }
            .Concat(versionCatalog.AdditionalVersionKeys.OrderBy(item => item, StringComparer.Ordinal))
            .Concat(versionCatalog.ImportJobIds.OrderBy(item => item).Select(item => item.ToString("N"))));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestSource)))[..16];
    }

    private static IReadOnlyList<SourceReference> BuildSourceReferences(Ruleset ruleset, MasterVersionSet versionSet, Guid? battleId, CalculationPreset? preset = null)
    {
        var references = new List<SourceReference>
        {
            new(ruleset.Slug, $"{ruleset.Title} {ruleset.Version}", "ruleset"),
            new(versionSet.Id.ToString("N"), versionSet.Label, "master-version-set"),
            new(versionSet.VersionCatalog.DamageRulesetVersion, "damage-ruleset", "ruleset-version"),
            new(versionSet.VersionCatalog.ExperienceRulesetVersion, "experience-ruleset", "ruleset-version"),
            new(versionSet.VersionCatalog.TypeChartVersion, "type-chart", "master-version")
        };

        if (battleId.HasValue)
        {
            references.Add(new SourceReference(battleId.Value.ToString("N"), "battle", "battle"));
        }

        if (preset is not null)
        {
            references.Add(new SourceReference(preset.Id.ToString("N"), $"preset-rev-{preset.Revision}", "calculation-preset"));
        }

        return references;
    }

    private static IReadOnlyList<SourceReference> BuildThresholdSearchSources(BattleDefinition battle, CalculationPreset? preset)
    {
        var references = new List<SourceReference>
        {
            new(battle.Id.ToString("N"), battle.Title, "battle")
        };

        if (preset is not null)
        {
            references.Add(new SourceReference(preset.Id.ToString("N"), $"preset-rev-{preset.Revision}", "calculation-preset"));
        }

        return references;
    }

    private static CalculationPreset? ResolvePreset(RunAggregate run, Guid? presetId)
        => presetId.HasValue
            ? run.Presets.SingleOrDefault(item => item.Id == presetId.Value)
                ?? throw new KeyNotFoundException("preset が見つかりません。")
            : null;

    private static PartyMemberDefinition ResolveProjectedAttacker(RunAggregate run, RouteProgressionProjection projection, Guid? playerPartyMemberId)
    {
        var initialState = run.InitialState ?? throw new InvalidOperationException("初期状態が未設定です。");
        var memberId = playerPartyMemberId
            ?? projection.PartyMembers.FirstOrDefault(item => item.IsBattleSimulatorEnabled)?.PartyMemberId
            ?? initialState.BaselineParty.First().PartyMemberId;
        return initialState.BaselineParty.Single(item => item.PartyMemberId == memberId);
    }
}
