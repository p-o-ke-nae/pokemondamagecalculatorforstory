using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        var updated = run with
        {
            InitialState = initialState with { Revision = (run.InitialState?.Revision ?? 0) + 1 },
            Routes = run.Routes.Select(route => route with { IsStale = true }).ToArray(),
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

        var route = RecalculateFingerprint(
            new RoutePlan(
                Guid.NewGuid(),
                runId,
                name.Trim(),
                false,
                "empty",
                Array.Empty<ProgressionEvent>(),
                Array.Empty<BattleDefinition>(),
                simulatedPartyMemberIds?.ToArray() ?? Array.Empty<Guid>(),
                null));

        var updated = run with
        {
            Routes = run.Routes.Append(route).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _runRepository.SaveRunAsync(updated, cancellationToken);
        return route;
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

        var updatedRoute = RecalculateFingerprint(route with
        {
            Events = route.Events.Append(eventToAdd).OrderBy(item => item.Sequence).ToArray(),
            IsStale = true
        });

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

        var updatedRoute = RecalculateFingerprint(route with
        {
            Events = updatedEvents,
            IsStale = true
        });

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

        var updatedRoute = RecalculateFingerprint(route with
        {
            Events = reordered,
            IsStale = true
        });

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

        var updatedRoute = RecalculateFingerprint(route with
        {
            Battles = route.Battles.Append(created).ToArray(),
            IsStale = true
        });

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

        var updatedRoute = RecalculateFingerprint(route with
        {
            Battles = updatedBattles,
            IsStale = true
        });

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
        var updatedRoute = RecalculateFingerprint(route with
        {
            Battles = route.Battles.Select(item => item.Id == battleId ? updatedBattle : item).ToArray(),
            IsStale = true
        });

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
            BuildSourceReferences(ruleset, versionSet, battle.Id));
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
        var run = await RequireOwnedRunAsync(request.RunId, cancellationToken);
        var route = run.Routes.SingleOrDefault(item => item.Id == request.RouteId)
            ?? throw new KeyNotFoundException("route が見つかりません。");
        var battle = route.Battles.SingleOrDefault(item => item.Id == request.BattleId)
            ?? throw new KeyNotFoundException("battle が見つかりません。");
        var defender = ResolveBattleEnemies(run, battle).FirstOrDefault()
            ?? throw new InvalidOperationException("battle に敵情報がありません。");
        var projection = await ProjectRouteAsync(run, route, cancellationToken);
        var attacker = ResolveProjectedAttacker(run, projection, request.PlayerPartyMemberId);

        var candidates = new List<ThresholdCandidate>();
        var rejected = new List<ThresholdCandidate>();
        for (var power = request.MovePowerRangeStart; power <= request.MovePowerRangeEnd; power++)
        {
            for (var attackBonus = 0; attackBonus <= request.MaximumAttackBonus; attackBonus++)
            {
                var result = await CalculateDamageAsync(
                    new DamageCalculationRequest(
                        run.Id,
                        route.Id,
                        battle.Id,
                        attacker.PartyMemberId,
                        request.MoveName,
                        power,
                        request.MoveType,
                        new CombatantSnapshot(attacker.Species, attacker.Level, attacker.CombatStats.Attack + attackBonus, attacker.CombatStats.Defense, attacker.Typing.PrimaryType, attacker.Typing.SecondaryType, attacker.HeldItem),
                        new CombatantSnapshot(defender.Species, defender.Level, defender.Attack, defender.Defense, defender.Typing.PrimaryType, defender.Typing.SecondaryType, null),
                        false,
                        null,
                        Array.Empty<DamageModifier>()),
                    cancellationToken);

                var candidate = new ThresholdCandidate(power, attackBonus, result.MinimumDamage,
                    result.MinimumDamage >= request.TargetMinimumDamage
                        ? "target achieved"
                        : "minimum damage below target");

                if (result.MinimumDamage >= request.TargetMinimumDamage)
                {
                    candidates.Add(candidate);
                }
                else
                {
                    rejected.Add(candidate);
                }
            }
        }

        return new ThresholdSearchResult(
            request.TargetMinimumDamage,
            candidates.Count > 0,
            candidates.OrderBy(item => item.MovePower).ThenBy(item => item.AttackBonus).ToArray(),
            rejected.OrderBy(item => item.MovePower).ThenBy(item => item.AttackBonus).Take(20).ToArray(),
            new[] { new SourceReference(battle.Id.ToString("N"), battle.Title, "battle") });
    }

    /// <summary>share snapshot を新規公開します。</summary>
    public async Task<SharedRouteSnapshot> CreateShareAsync(Guid runId, Guid routeId, string visibility, string summary, CancellationToken cancellationToken)
    {
        var (run, route) = await RequireOwnedRouteAsync(routeId, cancellationToken);
        if (run.Id != runId)
        {
            throw new InvalidOperationException("run と route の組み合わせが不正です。");
        }

        var user = RequireCurrentUser();
        var normalizedVisibility = NormalizeVisibility(visibility);
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var revision = await CreateRevisionAsync(run, route, user.UserId, string.IsNullOrWhiteSpace(summary) ? "initial publish" : summary.Trim(), null, cancellationToken);
        var share = new SharedRouteSnapshot(
            Guid.NewGuid(),
            user.UserId,
            run.Id,
            route.Id,
            normalizedVisibility,
            revision.Id,
            revision.Id,
            versionSet.VersionCatalog,
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
        var share = await RequireShareOwnerAsync(shareId, cancellationToken);
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
        var share = await GetShareAsync(shareId, cancellationToken);
        var (run, route) = await RequireOwnedRouteAsync(share.RouteId, cancellationToken);
        if (run.Id != share.RunId)
        {
            throw new InvalidOperationException("share に紐づく run と route が不整合です。");
        }

        var user = RequireCurrentUser();
        var revision = await CreateRevisionAsync(run, route, user.UserId, string.IsNullOrWhiteSpace(summary) ? "publish revision" : summary.Trim(), share.CurrentRevisionId, cancellationToken);
        var updated = share with
        {
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
        if (!string.Equals(baseSnapshot.Route.Name, targetSnapshot.Route.Name, StringComparison.Ordinal))
        {
            changedFields.Add("route.name");
        }

        if (baseSnapshot.Route.Events.Count != targetSnapshot.Route.Events.Count)
        {
            changedFields.Add("route.events.count");
        }

        if (baseSnapshot.Route.Battles.Count != targetSnapshot.Route.Battles.Count)
        {
            changedFields.Add("route.battles.count");
        }

        if ((baseSnapshot.InitialState?.Revision ?? 0) != (targetSnapshot.InitialState?.Revision ?? 0))
        {
            changedFields.Add("run.initial-state.revision");
        }

        var changedVersions = new List<string>();
        if (!string.Equals(baseSnapshot.VersionCatalog?.DamageRulesetVersion, targetSnapshot.VersionCatalog?.DamageRulesetVersion, StringComparison.Ordinal))
        {
            changedVersions.Add("damageRulesetVersion");
        }

        if (!string.Equals(baseSnapshot.VersionCatalog?.ExperienceRulesetVersion, targetSnapshot.VersionCatalog?.ExperienceRulesetVersion, StringComparison.Ordinal))
        {
            changedVersions.Add("experienceRulesetVersion");
        }

        return new ShareDiffResult(
            share.Id,
            baseRevision.Id,
            targetRevision.Id,
            changedFields.Count == 0 && changedVersions.Count == 0 ? "no structural diff" : $"{changedFields.Count + changedVersions.Count} field(s) changed",
            changedFields,
            changedVersions);
    }

    /// <summary>dry-run import job を作成します。</summary>
    public Task<ImportJob> CreateDryRunImportJobAsync(Guid rulesetId, string workbookName, CancellationToken cancellationToken)
        => CreateImportJobCoreAsync(rulesetId, workbookName, "dry-run", false, cancellationToken);

    /// <summary>commit import job を作成します。</summary>
    public Task<ImportJob> CreateCommitImportJobAsync(Guid rulesetId, string workbookName, CancellationToken cancellationToken)
        => CreateImportJobCoreAsync(rulesetId, workbookName, "commit", true, cancellationToken);

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

    private async Task<ImportJob> CreateImportJobCoreAsync(Guid rulesetId, string workbookName, string mode, bool publishVersionSet, CancellationToken cancellationToken)
    {
        var user = RequireCurrentUser();
        var ruleset = await _rulesetRepository.FindRulesetAsync(rulesetId, cancellationToken)
            ?? throw new KeyNotFoundException("指定された ruleset が見つかりません。");

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
                    Array.Empty<string>(),
                    Array.Empty<Guid>()),
                new[]
                {
                    new SourceReference("spreadsheet.sheet1", workbookName.Trim(), "spreadsheet"),
                    new SourceReference("import.mode", mode, "import")
                });

            publishedVersionSetId = versionSet.Id;
            await _rulesetRepository.SaveMasterVersionSetAsync(versionSet, cancellationToken);
        }

        var job = new ImportJob(
            Guid.NewGuid(),
            rulesetId,
            workbookName.Trim(),
            mode,
            "completed",
            user.UserId,
            publishVersionSet ? "import committed and version set prepared" : "dry-run completed",
            publishVersionSet
                ? new[] { "Rows validated.", "Master version set prepared." }
                : new[] { "Rows validated.", "No persistence side effects were applied." },
            publishedVersionSetId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

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

    private async Task<SharedRouteSnapshot> RequireShareReadAccessAsync(Guid shareId, CancellationToken cancellationToken)
    {
        var share = await _shareRepository.FindShareAsync(shareId, cancellationToken)
            ?? throw new KeyNotFoundException("share が見つかりません。");
        if (string.Equals(share.Visibility, "public", StringComparison.OrdinalIgnoreCase))
        {
            return share;
        }

        var currentUser = _currentUserAccessor.GetCurrentUser();
        if (currentUser is null || !string.Equals(currentUser.UserId, share.OwnerUserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("この share へのアクセス権がありません。");
        }

        return share;
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

    private static RoutePlan RecalculateFingerprint(RoutePlan route)
    {
        var fingerprintSource = string.Join(
            "|",
            route.Events.OrderBy(item => item.Sequence).Select(item => $"{item.Sequence}:{item.EventType}:{item.LinkedBattleId}:{item.Summary}:{item.Revision}")
            .Concat(route.Battles.OrderBy(item => item.Title).Select(item =>
                $"{item.Title}:{item.SourceKind}:{item.BattleKind}:{item.IsOptional}:{item.EnemyGroupId}:{string.Join(",", item.Participations.Select(participation => $"{participation.PartyMemberId}:{participation.ParticipationMode}:{participation.ShareRatio}"))}"))
            .Concat(route.SimulatedPartyMemberIds.OrderBy(item => item).Select(item => item.ToString("N"))));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource));
        return route with { ProgressionFingerprint = Convert.ToHexString(bytes[..8]) };
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

    private static string BuildEnemySummary(RunAggregate run, BattleDefinition battle)
    {
        var enemies = ResolveBattleEnemies(run, battle);
        return enemies.Count == 0
            ? "enemy unresolved"
            : string.Join(", ", enemies.Select(item => $"{item.Species} Lv{item.Level}"));
    }

    private async Task<RouteRevision> CreateRevisionAsync(RunAggregate run, RoutePlan route, string authorUserId, string summary, Guid? parentRevisionId, CancellationToken cancellationToken)
    {
        var versionSet = await _rulesetRepository.FindMasterVersionSetAsync(run.MasterVersionSetId, cancellationToken)
            ?? throw new KeyNotFoundException("master version set が見つかりません。");
        var projection = await ProjectRouteAsync(run, route, cancellationToken);
        var snapshotDocument = new RouteSnapshotDocument(route, run.InitialState, run.EnemyGroups, projection, versionSet.VersionCatalog);
        var snapshotJson = JsonSerializer.Serialize(snapshotDocument, SnapshotSerializerOptions);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));
        return new RouteRevision(Guid.NewGuid(), parentRevisionId, summary, snapshotJson, digest, authorUserId, DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<SourceReference> BuildSourceReferences(Ruleset ruleset, MasterVersionSet versionSet, Guid? battleId)
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

        return references;
    }

    private static PartyMemberDefinition ResolveProjectedAttacker(RunAggregate run, RouteProgressionProjection projection, Guid? playerPartyMemberId)
    {
        var initialState = run.InitialState ?? throw new InvalidOperationException("初期状態が未設定です。");
        var memberId = playerPartyMemberId
            ?? projection.PartyMembers.FirstOrDefault(item => item.IsBattleSimulatorEnabled)?.PartyMemberId
            ?? initialState.BaselineParty.First().PartyMemberId;
        return initialState.BaselineParty.Single(item => item.PartyMemberId == memberId);
    }
}
