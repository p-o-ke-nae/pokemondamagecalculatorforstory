using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

public sealed class Battle
{
    public Guid Id { get; private set; }
    public Guid RunId { get; private set; }
    public string EnemyPokemon { get; private set; } = string.Empty;
    public int Sequence { get; private set; }

    private Battle()
    {
    }

    public static Battle Create(Guid runId, string enemyPokemon, int sequence)
    {
        if (runId == Guid.Empty) throw new ValidationException("RunId is required.");
        if (string.IsNullOrWhiteSpace(enemyPokemon)) throw new ValidationException("EnemyPokemon is required.");
        if (sequence <= 0) throw new ValidationException("Sequence must be greater than zero.");

        return new Battle
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            EnemyPokemon = enemyPokemon.Trim(),
            Sequence = sequence
        };
    }

    public static Battle Restore(Guid id, Guid runId, string enemyPokemon, int sequence)
    {
        return new Battle
        {
            Id = id,
            RunId = runId,
            EnemyPokemon = enemyPokemon.Trim(),
            Sequence = sequence
        };
    }
}
