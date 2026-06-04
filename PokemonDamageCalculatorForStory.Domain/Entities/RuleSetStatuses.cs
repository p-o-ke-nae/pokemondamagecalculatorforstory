namespace PokemonDamageCalculatorForStory.Domain.Entities;

public static class RuleSetStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Archived = "Archived";

    public static readonly IReadOnlySet<string> AllowedValues = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
        Active,
        Archived
    };
}
