namespace PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

public class PersistedOwnPokemonSnapshot
{
    public Guid Id { get; set; }
    public Guid BattleId { get; set; }
    public Guid RunId { get; set; }
    public string Species { get; set; } = string.Empty;
    public int Level { get; set; }
    public string BaseStats { get; set; } = string.Empty;
    public string IVs { get; set; } = string.Empty;
    public string Stats { get; set; } = string.Empty;
    public string EVs { get; set; } = string.Empty;
}
