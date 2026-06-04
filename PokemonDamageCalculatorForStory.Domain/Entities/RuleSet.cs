using PokemonDamageCalculatorForStory.Domain.Exceptions;

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
        var ruleSet = new RuleSet
        {
            Id = Guid.NewGuid()
        };

        ruleSet.Apply(slug, generation, title, version, status, summary);
        return ruleSet;
    }

    public static RuleSet Restore(Guid id, string slug, int generation, string title, string version, string status, string summary)
    {
        var ruleSet = new RuleSet
        {
            Id = id
        };

        ruleSet.Apply(slug, generation, title, version, status, summary);
        return ruleSet;
    }

    public void Update(string slug, int generation, string title, string version, string status, string summary)
    {
        Apply(slug, generation, title, version, status, summary);
    }

    private void Apply(string slug, int generation, string title, string version, string status, string summary)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new ValidationException("Slug is required.");
        if (slug.Length > 100) throw new ValidationException("Slug must be 100 characters or fewer.");
        if (generation <= 0) throw new ValidationException("Generation must be positive.");
        if (string.IsNullOrWhiteSpace(title)) throw new ValidationException("Title is required.");
        if (title.Length > 200) throw new ValidationException("Title must be 200 characters or fewer.");
        if (string.IsNullOrWhiteSpace(version)) throw new ValidationException("Version is required.");
        if (version.Length > 50) throw new ValidationException("Version must be 50 characters or fewer.");
        if (string.IsNullOrWhiteSpace(status)) throw new ValidationException("Status is required.");
        if (!RuleSetStatuses.AllowedValues.Contains(status.Trim()))
        {
            throw new ValidationException("Status must be Draft, Active, or Archived.");
        }

        var normalizedSummary = string.IsNullOrWhiteSpace(summary) ? string.Empty : summary.Trim();
        if (normalizedSummary.Length > 500) throw new ValidationException("Summary must be 500 characters or fewer.");

        Slug = slug.Trim();
        Generation = generation;
        Title = title.Trim();
        Version = version.Trim();
        Status = status.Trim();
        Summary = normalizedSummary;
    }
}
