using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Web.Api;

internal sealed record CreateRunRequest(Guid RulesetId, string Name);

internal sealed record UpdateRunRequest(string Name, string Status);

internal sealed record UpsertInitialStateRequest(
    string PlayerSpecies,
    int Level,
    int Attack,
    int Defense,
    int Money,
    string? HeldItem,
    IReadOnlyList<string> Moves,
    string? Memo)
{
    public RunInitialState ToDomain()
        => new(0, PlayerSpecies, Level, Attack, Defense, Money, HeldItem, Moves, Memo);
}

internal sealed record CreateRouteRequest(string Name);

internal sealed record UpsertProgressionEventRequest(
    string EventType,
    string Summary,
    int LevelDelta,
    int MoneyDelta,
    string? SourceReference)
{
    public ProgressionEvent ToDomain()
        => new(Guid.Empty, 0, EventType, Summary, LevelDelta, MoneyDelta, SourceReference, 0);
}

internal sealed record ReorderRouteRequest(IReadOnlyList<Guid> EventIds);

internal sealed record EnemyCombatantRequest(
    string Species,
    int Level,
    int Hp,
    int Attack,
    int Defense,
    string? Note)
{
    public EnemyCombatant ToDomain()
        => new(Species, Level, Hp, Attack, Defense, Note);
}

internal sealed record UpsertEnemyGroupRequest(
    string Name,
    string SourceKind,
    IReadOnlyList<EnemyCombatantRequest> Members)
{
    public EnemyGroupDefinition ToDomain(Guid runId, Guid? groupId = null)
        => new(groupId ?? Guid.Empty, runId, Name, SourceKind, Members.Select(item => item.ToDomain()).ToArray());
}

internal sealed record UpsertBattleRequest(
    string Title,
    string SourceKind,
    string BattleKind,
    bool IsOptional,
    string? MasterBattleCode,
    Guid? EnemyGroupId,
    IReadOnlyList<EnemyCombatantRequest> InlineEnemies,
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
            Notes);
}

internal sealed record CombatantSnapshotRequest(
    string Species,
    int Level,
    int Attack,
    int Defense,
    string PrimaryType,
    string? HeldItem)
{
    public CombatantSnapshot ToDomain()
        => new(Species, Level, Attack, Defense, PrimaryType, HeldItem);
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
    string MoveName,
    int MovePower,
    string MoveType,
    CombatantSnapshotRequest Attacker,
    CombatantSnapshotRequest Defender,
    bool IsCritical,
    decimal TypeEffectiveness,
    IReadOnlyList<DamageModifierRequest>? AdditionalModifiers)
{
    public DamageCalculationRequest ToDomain()
        => new(
            RunId,
            RouteId,
            BattleId,
            MoveName,
            MovePower,
            MoveType,
            Attacker.ToDomain(),
            Defender.ToDomain(),
            IsCritical,
            TypeEffectiveness,
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

internal sealed record ThresholdSearchApiRequest(
    Guid RunId,
    Guid RouteId,
    Guid BattleId,
    string MoveName,
    string MoveType,
    int MovePowerRangeStart,
    int MovePowerRangeEnd,
    int MaximumAttackBonus,
    int TargetMinimumDamage)
{
    public ThresholdSearchRequest ToDomain()
        => new(
            RunId,
            RouteId,
            BattleId,
            MoveName,
            MoveType,
            MovePowerRangeStart,
            MovePowerRangeEnd,
            MaximumAttackBonus,
            TargetMinimumDamage);
}

internal sealed record CreateShareRequest(Guid RunId, Guid RouteId, string Visibility, string Summary);

internal sealed record AddCommentRequest(Guid? RevisionId, string Body);

internal sealed record PublishRevisionRequest(string Summary);

internal sealed record CreateImportJobRequest(Guid RulesetId, string WorkbookName);
