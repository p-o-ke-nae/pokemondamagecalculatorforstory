using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

public sealed class OwnPokemonSnapshot
{
    public const int SpeciesMaxLength = 100;

    public Guid Id { get; private set; }
    public Guid BattleId { get; private set; }
    public string Species { get; private set; } = string.Empty;
    public int Level { get; private set; }
    public PokemonStats Stats { get; private set; }
    public EffortValues EVs { get; private set; }

    private OwnPokemonSnapshot()
    {
    }

    public static OwnPokemonSnapshot Create(Guid battleId, string species, int level, PokemonStats stats, EffortValues eVs)
    {
        var validatedLevel = PokemonLevel.Create(level);

        if (battleId == Guid.Empty) throw new ValidationException("BattleId is required.");
        if (string.IsNullOrWhiteSpace(species)) throw new ValidationException("Species is required.");

        var normalizedSpecies = species.Trim();
        if (normalizedSpecies.Length > SpeciesMaxLength) throw new ValidationException($"Species must be {SpeciesMaxLength} characters or fewer.");

        return new OwnPokemonSnapshot
        {
            Id = Guid.NewGuid(),
            BattleId = battleId,
            Species = normalizedSpecies,
            Level = validatedLevel.Value,
            Stats = stats,
            EVs = eVs
        };
    }

    public static OwnPokemonSnapshot Restore(Guid id, Guid battleId, string species, int level, PokemonStats stats, EffortValues eVs)
    {
        return new OwnPokemonSnapshot
        {
            Id = id,
            BattleId = battleId,
            Species = species.Trim(),
            Level = level,
            Stats = stats,
            EVs = eVs
        };
    }
}
