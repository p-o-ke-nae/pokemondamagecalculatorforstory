namespace PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

internal sealed class PersistedRuleset
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;
}

internal sealed class PersistedMasterVersionSet
{
    public Guid Id { get; set; }

    public Guid RulesetId { get; set; }

    public bool IsPublished { get; set; }

    public string PayloadJson { get; set; } = string.Empty;
}

internal sealed class PersistedRun
{
    public Guid Id { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;
}

internal sealed class PersistedShare
{
    public Guid Id { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;
}

internal sealed class PersistedImportJob
{
    public Guid Id { get; set; }

    public string SubmittedByUserId { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;
}
