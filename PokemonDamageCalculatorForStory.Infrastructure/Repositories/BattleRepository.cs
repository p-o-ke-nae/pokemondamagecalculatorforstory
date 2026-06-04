using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Mappers;

namespace PokemonDamageCalculatorForStory.Infrastructure.Repositories;

public sealed class BattleRepository(AppDbContext context) : IBattleRepository
{
    public async Task<IReadOnlyList<Battle>> ListByRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedBattles.AsNoTracking().Where(x => x.RunId == runId).ToListAsync(cancellationToken);
        return persisted.Select(battle => battle.ToDomainEntity()).ToList().AsReadOnly();
    }

    public async Task<Battle?> FindAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedBattles.AsNoTracking().FirstOrDefaultAsync(x => x.RunId == runId && x.Id == battleId, cancellationToken);
        return persisted?.ToDomainEntity();
    }

    public async Task<Battle> SaveAsync(Battle battle, CancellationToken cancellationToken = default)
    {
        var existing = await context.PersistedBattles.FirstOrDefaultAsync(x => x.Id == battle.Id, cancellationToken);
        var persisted = battle.ToPersistedModel();

        if (existing is null)
        {
            context.PersistedBattles.Add(persisted);
        }
        else
        {
            existing.RunId = persisted.RunId;
            existing.EnemyPokemon = persisted.EnemyPokemon;
            existing.Sequence = persisted.Sequence;
        }

        await context.SaveChangesAsync(cancellationToken);
        return battle;
    }

    public async Task<bool> DeleteAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default)
    {
        var existing = await context.PersistedBattles.FirstOrDefaultAsync(x => x.RunId == runId && x.Id == battleId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        context.PersistedBattles.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<OwnPokemonSnapshot>> ListSnapshotsByRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedOwnPokemonSnapshots.AsNoTracking().Where(x => x.RunId == runId).ToListAsync(cancellationToken);
        return persisted.Select(snapshot => snapshot.ToDomainSnapshot()).ToList().AsReadOnly();
    }

    public async Task<OwnPokemonSnapshot> SaveSnapshotAsync(OwnPokemonSnapshot snapshot, Guid runId, CancellationToken cancellationToken = default)
    {
        var persisted = snapshot.ToPersistedSnapshot(runId);
        context.PersistedOwnPokemonSnapshots.Add(persisted);
        await context.SaveChangesAsync(cancellationToken);
        return snapshot;
    }

    public async Task<CalculationResult> SaveCalculationResultAsync(CalculationResult result, CancellationToken cancellationToken = default)
    {
        context.PersistedCalculationResults.Add(result.ToPersistedCalculationResult());
        await context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
