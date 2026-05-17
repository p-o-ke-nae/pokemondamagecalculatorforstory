using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Application.Services;

/// <summary>手持ち進捗を再導出します。</summary>
public sealed class StoryProgressionProjector
{
    /// <summary>route の進捗投影を作成します。</summary>
    /// <param name="run">対象 run です。</param>
    /// <param name="route">対象 route です。</param>
    /// <param name="ruleset">対象 ruleset です。</param>
    /// <param name="versionSet">固定 version set です。</param>
    /// <returns>進捗投影です。</returns>
    public RouteProgressionProjection Project(RunAggregate run, RoutePlan route, Ruleset ruleset, MasterVersionSet versionSet)
    {
        if (run.InitialState is null)
        {
            return new RouteProgressionProjection(
                route.Id,
                DateTimeOffset.UtcNow,
                Array.Empty<PartyMemberProjection>(),
                new[] { new VerificationMessage("initial-state.missing", "初期状態が未設定です。", null) },
                route.ProgressionFingerprint,
                BuildSourceReferences(ruleset, versionSet));
        }

        var partyState = run.InitialState.BaselineParty.ToDictionary(
            member => member.PartyMemberId,
            ClonePartyMember);
        var warnings = new List<VerificationMessage>();

        foreach (var progressionEvent in route.Events.OrderBy(item => item.Sequence))
        {
            var battle = progressionEvent.LinkedBattleId.HasValue
                ? route.Battles.SingleOrDefault(item => item.Id == progressionEvent.LinkedBattleId.Value)
                : null;
            var derivedBattleDeltas = battle is null
                ? new Dictionary<Guid, PartyProgressionDelta>()
                : BuildBattleDerivedDeltas(run, battle, ruleset);

            foreach (var partyDelta in progressionEvent.PartyDeltas)
            {
                if (!partyState.TryGetValue(partyDelta.PartyMemberId, out var member))
                {
                    warnings.Add(new VerificationMessage("party.missing", $"event '{progressionEvent.Summary}' が存在しない party member を参照しています。", progressionEvent.LinkedBattleId));
                    continue;
                }

                derivedBattleDeltas.TryGetValue(partyDelta.PartyMemberId, out var derivedDelta);
                member = ApplyDelta(member, derivedDelta);
                member = ApplyDelta(member, partyDelta);
                partyState[partyDelta.PartyMemberId] = member;
            }

            foreach (var (partyMemberId, derivedDelta) in derivedBattleDeltas)
            {
                if (progressionEvent.PartyDeltas.Any(item => item.PartyMemberId == partyMemberId))
                {
                    continue;
                }

                if (!partyState.TryGetValue(partyMemberId, out var member))
                {
                    continue;
                }

                partyState[partyMemberId] = ApplyDelta(member, derivedDelta);
            }
        }

        var projections = partyState.Values
            .OrderBy(item => item.Slot)
            .Select(member =>
            {
                var memberWarnings = member.Moves
                    .Where(move => move.CurrentPp < 0)
                    .Select(move => new VerificationMessage("pp.negative", $"{member.Species} の {move.MoveName} の PP が不足しています。", null))
                    .ToArray();
                warnings.AddRange(memberWarnings);

                var isSimulated = route.SimulatedPartyMemberIds.Count == 0 || route.SimulatedPartyMemberIds.Contains(member.PartyMemberId);
                return new PartyMemberProjection(
                    member.PartyMemberId,
                    member.Slot,
                    member.Species,
                    member.Level,
                    member.Experience,
                    member.EffortValues,
                    member.Moves,
                    isSimulated ? "active" : "baseline-only",
                    member.IsBattleSimulatorEnabled,
                    memberWarnings);
            })
            .ToArray();

        return new RouteProgressionProjection(
            route.Id,
            DateTimeOffset.UtcNow,
            projections,
            warnings,
            route.ProgressionFingerprint,
            BuildSourceReferences(ruleset, versionSet));
    }

    /// <summary>指定ポケモンの現在 PP 不足警告を取得します。</summary>
    /// <param name="projection">進捗投影です。</param>
    /// <param name="partyMemberId">対象ポケモン識別子です。</param>
    /// <param name="moveName">対象技名です。</param>
    /// <returns>警告メッセージ一覧です。</returns>
    public IReadOnlyList<string> GetPpWarnings(RouteProgressionProjection projection, Guid? partyMemberId, string moveName)
    {
        if (!partyMemberId.HasValue)
        {
            return Array.Empty<string>();
        }

        var member = projection.PartyMembers.SingleOrDefault(item => item.PartyMemberId == partyMemberId.Value);
        if (member is null)
        {
            return Array.Empty<string>();
        }

        var move = member.Moves.SingleOrDefault(item => string.Equals(item.MoveName, moveName, StringComparison.OrdinalIgnoreCase));
        if (move is null || move.CurrentPp >= 0)
        {
            return Array.Empty<string>();
        }

        return new[] { $"{member.Species} の {move.MoveName} は PP が {move.CurrentPp} です。計算は継続しますが route breakage warning として扱います。" };
    }

    private static PartyMemberDefinition ClonePartyMember(PartyMemberDefinition member)
        => member with
        {
            IndividualValues = member.IndividualValues with { },
            EffortValues = member.EffortValues with { },
            CombatStats = member.CombatStats with { },
            Typing = member.Typing with { },
            Moves = member.Moves.Select(move => move with { }).ToArray()
        };

    private static PartyMemberDefinition ApplyDelta(PartyMemberDefinition member, PartyProgressionDelta? delta)
    {
        if (delta is null)
        {
            return member;
        }

        var updatedMoves = member.Moves
            .Select(move =>
            {
                var ppDelta = delta.PpDeltas.SingleOrDefault(item => string.Equals(item.MoveName, move.MoveName, StringComparison.OrdinalIgnoreCase));
                return ppDelta is null ? move : move with { CurrentPp = move.CurrentPp + ppDelta.Delta };
            })
            .ToArray();

        if (delta.ReplaceMoves is { Count: > 0 })
        {
            updatedMoves = delta.ReplaceMoves.Select(move => move with { }).ToArray();
        }

        return member with
        {
            Species = string.IsNullOrWhiteSpace(delta.SpeciesOverride) ? member.Species : delta.SpeciesOverride.Trim(),
            Level = member.Level + delta.RareCandyLevels,
            Experience = Math.Max(0, member.Experience + delta.ExperienceDelta),
            EffortValues = AddStats(member.EffortValues, delta.EffortValueDelta),
            Ability = string.IsNullOrWhiteSpace(delta.AbilityOverride) ? member.Ability : delta.AbilityOverride.Trim(),
            Nature = string.IsNullOrWhiteSpace(delta.NatureOverride) ? member.Nature : delta.NatureOverride.Trim(),
            HeldItem = delta.HeldItemOverride ?? member.HeldItem,
            Moves = updatedMoves
        };
    }

    private static Dictionary<Guid, PartyProgressionDelta> BuildBattleDerivedDeltas(RunAggregate run, BattleDefinition battle, Ruleset ruleset)
    {
        if (run.InitialState is null)
        {
            return new Dictionary<Guid, PartyProgressionDelta>();
        }

        var enemyYield = battle.EnemyGroupId.HasValue
            ? run.EnemyGroups.SingleOrDefault(group => group.Id == battle.EnemyGroupId.Value)?.Members ?? Array.Empty<EnemyCombatant>()
            : battle.InlineEnemies;
        var totalBaseExperience = enemyYield.Sum(enemy => enemy.BaseExperienceYield * Math.Max(1, enemy.Level));
        var totalEvYield = enemyYield.Aggregate(new StatValues(0, 0, 0, 0, 0, 0), (current, enemy) => AddStats(current, enemy.EffortValueYield));

        return battle.Participations.ToDictionary(
            participation => participation.PartyMemberId,
            participation =>
            {
                var ratio = ResolveParticipationRatio(participation);
                var experience = CalculateExperienceFromRules(totalBaseExperience, ratio, ruleset);
                var effortValues = MultiplyStats(totalEvYield, ratio);
                return new PartyProgressionDelta(
                    participation.PartyMemberId,
                    experience,
                    effortValues,
                    Array.Empty<MovePpDelta>(),
                    0,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "battle-derived");
            });
    }

    private static decimal ResolveParticipationRatio(BattleParticipationPlan participation)
    {
        if (participation.OutcomeChecklist.IntentionalLoss || participation.OutcomeChecklist.DidNotDefeat)
        {
            return 0m;
        }

        return participation.ParticipationMode.ToLowerInvariant() switch
        {
            "active" => 1m,
            "reserve" => 0.5m,
            "shared" => participation.ShareRatio <= 0m ? 0.5m : participation.ShareRatio,
            _ => 0m
        };
    }

    private static int CalculateExperienceFromRules(int totalBaseExperience, decimal ratio, Ruleset ruleset)
    {
        var baseValue = ruleset.Generation switch
        {
            "Gen3" => totalBaseExperience / 7m,
            "Gen4" => totalBaseExperience / 7m,
            "Gen5" => totalBaseExperience / 6.5m,
            "Gen6" => totalBaseExperience / 6m,
            "Gen7" => totalBaseExperience / 6m,
            "Gen8" => totalBaseExperience / 5.8m,
            "Gen9" => totalBaseExperience / 5.8m,
            _ => totalBaseExperience / 6m
        };

        return (int)Math.Floor(baseValue * ratio);
    }

    private static StatValues MultiplyStats(StatValues values, decimal ratio)
        => new(
            (int)Math.Floor(values.Hp * ratio),
            (int)Math.Floor(values.Attack * ratio),
            (int)Math.Floor(values.Defense * ratio),
            (int)Math.Floor(values.SpecialAttack * ratio),
            (int)Math.Floor(values.SpecialDefense * ratio),
            (int)Math.Floor(values.Speed * ratio));

    private static StatValues AddStats(StatValues left, StatValues right)
        => new(
            left.Hp + right.Hp,
            left.Attack + right.Attack,
            left.Defense + right.Defense,
            left.SpecialAttack + right.SpecialAttack,
            left.SpecialDefense + right.SpecialDefense,
            left.Speed + right.Speed);

    private static IReadOnlyList<SourceReference> BuildSourceReferences(Ruleset ruleset, MasterVersionSet versionSet)
        =>
        [
            new SourceReference(ruleset.Slug, $"{ruleset.Title} {ruleset.Version}", "ruleset"),
            new SourceReference(versionSet.VersionCatalog.ExperienceRulesetVersion, "experience-ruleset", "ruleset-version"),
            new SourceReference(versionSet.VersionCatalog.PpRuleVersion, "pp-rule", "ruleset-version")
        ];
}
