namespace PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

public class PersistedBattle
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string EnemyPokemon { get; set; } = string.Empty;
    public int Sequence { get; set; }
}
