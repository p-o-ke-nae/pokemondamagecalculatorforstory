namespace PokemonDamageCalculatorForStory.Application.Auditing;

public sealed record AdminAuditEntry(
    DateTimeOffset OccurredAtUtc,
    string ActorGoogleUserId,
    string ActorRole,
    string Operation,
    string TargetType,
    string TargetId,
    IReadOnlyDictionary<string, object?> Payload,
    string Result);
