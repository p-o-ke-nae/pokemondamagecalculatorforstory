namespace PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

public class PersistedRuleSet
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int Generation { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
