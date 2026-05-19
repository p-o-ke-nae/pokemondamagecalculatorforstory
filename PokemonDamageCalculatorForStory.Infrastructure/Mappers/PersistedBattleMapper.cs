using System.Text.Json;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Mappers;

public static class PersistedBattleMapper
{
    public static Battle ToDomainEntity(this PersistedBattle persisted)
        => Battle.Restore(persisted.Id, persisted.RunId, persisted.EnemyPokemon, persisted.Sequence);

    public static PersistedBattle ToPersistedModel(this Battle entity)
        => new()
        {
            Id = entity.Id,
            RunId = entity.RunId,
            EnemyPokemon = entity.EnemyPokemon,
            Sequence = entity.Sequence
        };

    public static OwnPokemonSnapshot ToDomainSnapshot(this PersistedOwnPokemonSnapshot persisted)
        => OwnPokemonSnapshot.Restore(
            persisted.Id,
            persisted.BattleId,
            persisted.Species,
            persisted.Level,
            persisted.BaseStats is null ? null : SpeciesBaseStats.FromJson(persisted.BaseStats),
            persisted.IVs is null ? null : IndividualValues.FromJson(persisted.IVs),
            PokemonStats.FromJson(persisted.Stats),
            EffortValues.FromJson(persisted.EVs));

    public static PersistedOwnPokemonSnapshot ToPersistedSnapshot(this OwnPokemonSnapshot entity, Guid runId)
        => new()
        {
            Id = entity.Id,
            RunId = runId,
            BattleId = entity.BattleId,
            Species = entity.Species,
            Level = entity.Level,
            BaseStats = entity.BaseStats?.ToJson(),
            IVs = entity.IVs?.ToJson(),
            Stats = entity.Stats.ToJson(),
            EVs = entity.EVs.ToJson()
        };

    public static CalculationResult ToDomainCalculationResult(this PersistedCalculationResult persisted)
    {
        var rolls = JsonSerializer.Deserialize<int[]>(persisted.DamageRollsJson) ?? [];
        return CalculationResult.Restore(persisted.Id, persisted.RunId, persisted.BattleId, persisted.AttackerParams, persisted.DefenderParams, rolls);
    }

    public static PersistedCalculationResult ToPersistedCalculationResult(this CalculationResult entity)
        => new()
        {
            Id = entity.Id,
            RunId = entity.RunId,
            BattleId = entity.BattleId,
            AttackerParams = entity.AttackerParams,
            DefenderParams = entity.DefenderParams,
            DamageRollsJson = JsonSerializer.Serialize(entity.DamageRolls)
        };
}
