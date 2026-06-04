namespace PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

public class PersistedRun
{
    public Guid Id { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public Guid RuleSetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
