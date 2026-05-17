using System.Text.Json;
using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Web.Api;

internal sealed record CreateRunRequest(Guid RulesetId, string Name);

internal sealed record UpdateRunRequest(string Name, string Status);

internal sealed record StatValuesRequest(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed)
{
    public StatValues ToDomain()
        => new(Hp, Attack, Defense, SpecialAttack, SpecialDefense, Speed);
}

internal sealed record CombatStatSnapshotRequest(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed)
{
    public CombatStatSnapshot ToDomain()
        => new(Hp, Attack, Defense, SpecialAttack, SpecialDefense, Speed);
}

internal sealed record MoveStateRequest(
    string MoveName,
    int MaxPp,
    int CurrentPp)
{
    public MoveState ToDomain()
        => new(MoveName, MaxPp, CurrentPp);
}

internal sealed record PokemonTypeSlotRequest(
    string PrimaryType,
    string? SecondaryType)
{
    public PokemonTypeSlot ToDomain()
        => new(StoryApiTypeParser.ParsePokemonType(PrimaryType), string.IsNullOrWhiteSpace(SecondaryType) ? null : StoryApiTypeParser.ParsePokemonType(SecondaryType));
}

internal sealed record PartyMemberRequest(
    Guid? PartyMemberId,
    int Slot,
    string Species,
    int Level,
    int Experience,
    StatValuesRequest IndividualValues,
    StatValuesRequest EffortValues,
    string Nature,
    string Ability,
    CombatStatSnapshotRequest CombatStats,
    PokemonTypeSlotRequest Typing,
    string? HeldItem,
    IReadOnlyList<MoveStateRequest> Moves,
    string? Memo,
    bool IsBattleSimulatorEnabled)
{
    public PartyMemberDefinition ToDomain()
        => new(
            PartyMemberId ?? Guid.NewGuid(),
            Slot,
            Species,
            Level,
            Experience,
            IndividualValues.ToDomain(),
            EffortValues.ToDomain(),
            Nature,
            Ability,
            CombatStats.ToDomain(),
            Typing.ToDomain(),
            HeldItem,
            Moves.Select(item => item.ToDomain()).ToArray(),
            Memo,
            IsBattleSimulatorEnabled);
}

internal sealed record UpsertInitialStateRequest(
    int BaselineMoney,
    IReadOnlyList<PartyMemberRequest> BaselineParty,
    string? Memo)
{
    public RunInitialState ToDomain()
        => new(0, BaselineMoney, BaselineParty.Select(item => item.ToDomain()).ToArray(), Memo);
}

internal sealed record CreateRouteRequest(string Name, IReadOnlyList<Guid>? SimulatedPartyMemberIds);

internal sealed record MovePpDeltaRequest(
    string MoveName,
    int Delta)
{
    public MovePpDelta ToDomain()
        => new(MoveName, Delta);
}

internal sealed record PartyProgressionDeltaRequest(
    Guid PartyMemberId,
    int ExperienceDelta,
    StatValuesRequest EffortValueDelta,
    IReadOnlyList<MovePpDeltaRequest>? PpDeltas,
    int RareCandyLevels,
    string? SpeciesOverride,
    string? AbilityOverride,
    string? NatureOverride,
    string? HeldItemOverride,
    IReadOnlyList<MoveStateRequest>? ReplaceMoves,
    string? Notes)
{
    public PartyProgressionDelta ToDomain()
        => new(
            PartyMemberId,
            ExperienceDelta,
            EffortValueDelta.ToDomain(),
            PpDeltas?.Select(item => item.ToDomain()).ToArray() ?? Array.Empty<MovePpDelta>(),
            RareCandyLevels,
            SpeciesOverride,
            AbilityOverride,
            NatureOverride,
            HeldItemOverride,
            ReplaceMoves?.Select(item => item.ToDomain()).ToArray(),
            Notes);
}

internal sealed record UpsertProgressionEventRequest(
    string EventType,
    string Summary,
    Guid? LinkedBattleId,
    int MoneyDelta,
    string? SourceReference,
    IReadOnlyList<PartyProgressionDeltaRequest>? PartyDeltas)
{
    public ProgressionEvent ToDomain()
        => new(
            Guid.Empty,
            0,
            EventType,
            Summary,
            LinkedBattleId,
            MoneyDelta,
            SourceReference,
            PartyDeltas?.Select(item => item.ToDomain()).ToArray() ?? Array.Empty<PartyProgressionDelta>(),
            0);
}

internal sealed record ReorderRouteRequest(IReadOnlyList<Guid> EventIds);

internal sealed record EnemyCombatantRequest(
    string Species,
    int Level,
    int Hp,
    int Attack,
    int Defense,
    string PrimaryType,
    string? SecondaryType,
    int BaseExperienceYield,
    StatValuesRequest EffortValueYield,
    string? Note)
{
    public EnemyCombatant ToDomain()
        => new(Species, Level, Hp, Attack, Defense, new PokemonTypeSlot(StoryApiTypeParser.ParsePokemonType(PrimaryType), string.IsNullOrWhiteSpace(SecondaryType) ? null : StoryApiTypeParser.ParsePokemonType(SecondaryType)), BaseExperienceYield, EffortValueYield.ToDomain(), Note);
}

internal sealed record UpsertEnemyGroupRequest(
    string Name,
    string SourceKind,
    IReadOnlyList<EnemyCombatantRequest> Members)
{
    public EnemyGroupDefinition ToDomain(Guid runId, Guid? groupId = null)
        => new(groupId ?? Guid.Empty, runId, Name, SourceKind, Members.Select(item => item.ToDomain()).ToArray());
}

internal sealed record ManualOutcomeChecklistRequest(
    bool SentOutAndDefeated,
    bool DefeatedWhileInReserve,
    bool DidNotDefeat,
    bool IntentionalLoss)
{
    public ManualOutcomeChecklist ToDomain()
        => new(SentOutAndDefeated, DefeatedWhileInReserve, DidNotDefeat, IntentionalLoss);
}

internal sealed record BattleParticipationPlanRequest(
    Guid PartyMemberId,
    string ParticipationMode,
    decimal ShareRatio,
    string? SuggestedRole,
    ManualOutcomeChecklistRequest OutcomeChecklist)
{
    public BattleParticipationPlan ToDomain()
        => new(PartyMemberId, ParticipationMode, ShareRatio, SuggestedRole, OutcomeChecklist.ToDomain());
}

internal sealed record UpsertBattleRequest(
    string Title,
    string SourceKind,
    string BattleKind,
    bool IsOptional,
    string? MasterBattleCode,
    Guid? EnemyGroupId,
    IReadOnlyList<EnemyCombatantRequest> InlineEnemies,
    IReadOnlyList<Guid>? SuggestedPartyMemberIds,
    IReadOnlyList<BattleParticipationPlanRequest>? Participations,
    string? Notes)
{
    public BattleDefinition ToDomain(Guid routeId, Guid? battleId = null)
        => new(
            battleId ?? Guid.Empty,
            routeId,
            Title,
            SourceKind,
            BattleKind,
            IsOptional,
            MasterBattleCode,
            EnemyGroupId,
            InlineEnemies.Select(item => item.ToDomain()).ToArray(),
            SuggestedPartyMemberIds?.ToArray() ?? Array.Empty<Guid>(),
            Participations?.Select(item => item.ToDomain()).ToArray() ?? Array.Empty<BattleParticipationPlan>(),
            Notes);
}

internal sealed record UpdateBattleParticipationRequest(
    IReadOnlyList<Guid>? SuggestedPartyMemberIds,
    IReadOnlyList<BattleParticipationPlanRequest> Participations);

internal sealed record CombatantSnapshotRequest(
    string Species,
    int Level,
    int Attack,
    int Defense,
    string PrimaryType,
    string? SecondaryType,
    string? HeldItem)
{
    public CombatantSnapshot ToDomain()
        => new(Species, Level, Attack, Defense, StoryApiTypeParser.ParsePokemonType(PrimaryType), string.IsNullOrWhiteSpace(SecondaryType) ? null : StoryApiTypeParser.ParsePokemonType(SecondaryType), HeldItem);
}

internal sealed record DamageModifierRequest(
    string Code,
    string Label,
    decimal Multiplier,
    string SourceCode,
    string SourceLabel,
    string SourceType)
{
    public DamageModifier ToDomain()
        => new(Code, Label, Multiplier, new SourceReference(SourceCode, SourceLabel, SourceType));
}

internal sealed record DamageCalculationApiRequest(
    Guid RunId,
    Guid RouteId,
    Guid BattleId,
    Guid? PlayerPartyMemberId,
    Guid? PresetId,
    string MoveName,
    int MovePower,
    string MoveType,
    CombatantSnapshotRequest Attacker,
    CombatantSnapshotRequest Defender,
    bool IsCritical,
    decimal? TypeEffectivenessOverride,
    IReadOnlyList<DamageModifierRequest>? AdditionalModifiers)
{
    public DamageCalculationRequest ToDomain()
        => new(
            RunId,
            RouteId,
            BattleId,
            PlayerPartyMemberId,
            PresetId,
            MoveName,
            MovePower,
            StoryApiTypeParser.ParsePokemonType(MoveType),
            Attacker.ToDomain(),
            Defender.ToDomain(),
            IsCritical,
            TypeEffectivenessOverride,
            AdditionalModifiers?.Select(item => item.ToDomain()).ToArray() ?? Array.Empty<DamageModifier>());
}

internal sealed record ComparePatternItemRequest(
    string PatternKey,
    int MovePowerDelta,
    int AttackBonus,
    IReadOnlyList<DamageModifierRequest>? AdditionalModifiers);

internal sealed record ComparePatternsRequest(
    DamageCalculationApiRequest BaseCase,
    IReadOnlyList<ComparePatternItemRequest> Patterns);

internal sealed record ThresholdConditionRequest(
    string ConditionKey,
    string ConditionType,
    int ExpectedValue)
{
    public ThresholdCondition ToDomain()
        => new(ConditionKey, ConditionType, ExpectedValue);
}

internal sealed record ThresholdSearchApiRequest(
    Guid RunId,
    Guid RouteId,
    Guid BattleId,
    Guid? PlayerPartyMemberId,
    Guid? PresetId,
    string MoveName,
    int MovePower,
    string MoveType,
    IReadOnlyList<string> SearchStats,
    string ConditionMode,
    IReadOnlyList<ThresholdConditionRequest> Conditions,
    IReadOnlyList<string>? PriorityOrder)
{
    public ThresholdSearchRequest ToDomain()
        => new(
            RunId,
            RouteId,
            BattleId,
            PlayerPartyMemberId,
            PresetId,
            MoveName,
            MovePower,
            StoryApiTypeParser.ParsePokemonType(MoveType),
            SearchStats,
            ConditionMode,
            Conditions.Select(item => item.ToDomain()).ToArray(),
            PriorityOrder?.ToArray() ?? Array.Empty<string>());
}

internal sealed record CreateShareRequest(
    Guid RunId,
    string SourceType,
    Guid SourceId,
    string Visibility,
    IReadOnlyList<string>? AllowedRoles,
    string Summary,
    JsonElement? FrozenInput,
    JsonElement? FrozenOutput);

internal sealed record AddCommentRequest(Guid? RevisionId, string Body);

internal sealed record PublishRevisionRequest(string Summary);

internal sealed record CreateImportJobRequest(Guid RulesetId, string WorkbookName, string? Mode, string? SourceType, string? WorkbookContent);

internal sealed record StatRangeRequest(string Stat, int Minimum, int Maximum)
{
    public StatRange ToDomain() => new(Stat, Minimum, Maximum);
}

internal sealed record EffortValuePatternRequest(string PatternKey, StatValuesRequest EffortValues, string? Notes)
{
    public EffortValuePattern ToDomain() => new(PatternKey, EffortValues.ToDomain(), Notes);
}

internal sealed record NaturePatternRequest(string PatternKey, string Nature, string? Notes)
{
    public NaturePattern ToDomain() => new(PatternKey, Nature, Notes);
}

internal sealed record UpsertPresetRequest(
    Guid RunId,
    IReadOnlyList<StatRangeRequest> IvRanges,
    IReadOnlyList<EffortValuePatternRequest> EvPatterns,
    IReadOnlyList<NaturePatternRequest> NaturePatterns,
    string? Notes)
{
    public CalculationPreset ToDomain(Guid? presetId = null, int revision = 0)
        => new(
            presetId ?? Guid.Empty,
            RunId,
            revision,
            IvRanges.Select(item => item.ToDomain()).ToArray(),
            EvPatterns.Select(item => item.ToDomain()).ToArray(),
            NaturePatterns.Select(item => item.ToDomain()).ToArray(),
            Notes);
}

internal static class StoryApiTypeParser
{
    public static PokemonType ParsePokemonType(string rawType)
    {
        if (!Enum.TryParse<PokemonType>(rawType, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException($"未知のポケモンタイプです: {rawType}");
        }

        return parsed;
    }
}
