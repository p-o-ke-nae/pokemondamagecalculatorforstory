namespace PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

public class PersistedCalculationResult
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid BattleId { get; set; }
    public string AttackerParams { get; set; } = string.Empty;
    public string DefenderParams { get; set; } = string.Empty;
    public string DamageRollsJson { get; set; } = string.Empty;
}
