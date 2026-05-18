using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Mappers;

namespace PokemonDamageCalculatorForStory.Infrastructure.Repositories;

public sealed class RunRepository(AppDbContext context) : IRunRepository
{
    public async Task<IReadOnlyList<Run>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuns.AsNoTracking().ToListAsync(cancellationToken);
        return persisted.Select(run => run.ToDomainEntity()).ToList().AsReadOnly();
    }

    public async Task<Run?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return persisted?.ToDomainEntity();
    }

    public async Task<Run> SaveAsync(Run run, CancellationToken cancellationToken = default)
    {
        var existing = await context.PersistedRuns.FirstOrDefaultAsync(x => x.Id == run.Id, cancellationToken);
        var persisted = run.ToPersistedModel();

        if (existing is null)
        {
            context.PersistedRuns.Add(persisted);
        }
        else
        {
            existing.OwnerUserId = persisted.OwnerUserId;
            existing.RuleSetId = persisted.RuleSetId;
            existing.Name = persisted.Name;
            existing.Status = persisted.Status;
        }

        await context.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await context.PersistedRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        context.PersistedRuns.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
