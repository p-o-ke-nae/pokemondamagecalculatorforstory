namespace PokemonDamageCalculatorForStory.Domain.Entities;

public sealed class OwnPokemonSnapshot
{
    public Guid Id { get; private set; }
    public Guid BattleId { get; private set; }
    public string Species { get; private set; } = string.Empty;
    public int Level { get; private set; }
    public string Stats { get; private set; } = string.Empty;
    public string EVs { get; private set; } = string.Empty;

    private OwnPokemonSnapshot()
    {
    }

    public static OwnPokemonSnapshot Create(Guid battleId, string species, int level, string stats, string eVs)
    {
        if (battleId == Guid.Empty) throw new InvalidOperationException("BattleId is required.");
        if (string.IsNullOrWhiteSpace(species)) throw new InvalidOperationException("Species is required.");
        if (level <= 0) throw new InvalidOperationException("Level must be greater than zero.");
        if (string.IsNullOrWhiteSpace(stats)) throw new InvalidOperationException("Stats is required.");
        if (string.IsNullOrWhiteSpace(eVs)) throw new InvalidOperationException("EVs is required.");

        return new OwnPokemonSnapshot
        {
            Id = Guid.NewGuid(),
            BattleId = battleId,
            Species = species.Trim(),
            Level = level,
            Stats = stats.Trim(),
            EVs = eVs.Trim()
        };
    }

    public static OwnPokemonSnapshot Restore(Guid id, Guid battleId, string species, int level, string stats, string eVs)
    {
        var entity = Create(battleId, species, level, stats, eVs);
        entity.Id = id;
        return entity;
    }
}
