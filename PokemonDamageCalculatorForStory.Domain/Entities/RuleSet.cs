namespace PokemonDamageCalculatorForStory.Domain.Entities;

public sealed class RuleSet
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public int Generation { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;

    private RuleSet()
    {
    }

    public static RuleSet Create(string slug, int generation, string title, string version, string status, string summary)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new InvalidOperationException("Slug is required.");
        if (generation <= 0) throw new InvalidOperationException("Generation must be positive.");
        if (string.IsNullOrWhiteSpace(title)) throw new InvalidOperationException("Title is required.");
        if (string.IsNullOrWhiteSpace(version)) throw new InvalidOperationException("Version is required.");
        if (string.IsNullOrWhiteSpace(status)) throw new InvalidOperationException("Status is required.");

        return new RuleSet
        {
            Id = Guid.NewGuid(),
            Slug = slug.Trim(),
            Generation = generation,
            Title = title.Trim(),
            Version = version.Trim(),
            Status = status.Trim(),
            Summary = summary.Trim()
        };
    }

    public static RuleSet Restore(Guid id, string slug, int generation, string title, string version, string status, string summary)
    {
        var entity = Create(slug, generation, title, version, status, summary);
        entity.Id = id;
        return entity;
    }
}
