using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Mappers;

namespace PokemonDamageCalculatorForStory.Infrastructure.Repositories;

public sealed class RuleSetRepository(AppDbContext context) : IRuleSetRepository
{
    public async Task<IReadOnlyList<RuleSet>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuleSets.AsNoTracking().ToListAsync(cancellationToken);
        return persisted.Select(ruleSet => ruleSet.ToDomainEntity()).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RuleSet>> FindAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuleSets
            .AsNoTracking()
            .Where(x => x.Status == Domain.Entities.RuleSetStatuses.Active)
            .ToListAsync(cancellationToken);

        return persisted.Select(ruleSet => ruleSet.ToDomainEntity()).ToList().AsReadOnly();
    }

    public async Task<RuleSet?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuleSets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return persisted?.ToDomainEntity();
    }

    public async Task<RuleSet?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuleSets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == Domain.Entities.RuleSetStatuses.Active, cancellationToken);

        return persisted?.ToDomainEntity();
    }

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var query = context.PersistedRuleSets.AsQueryable().Where(x => x.Slug == slug);

        if (excludingId.HasValue)
        {
            query = query.Where(x => x.Id != excludingId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsReferencedByRunsAsync(Guid id, CancellationToken cancellationToken = default)
        => context.PersistedRuns.AsNoTracking().AnyAsync(run => run.RuleSetId == id, cancellationToken);

    public async Task<IReadOnlySet<Guid>> FindReferencedRuleSetIdsAsync(CancellationToken cancellationToken = default)
        => (await context.PersistedRuns
                .AsNoTracking()
                .Select(run => run.RuleSetId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

    public async Task AddAsync(RuleSet ruleSet, CancellationToken cancellationToken = default)
    {
        await context.PersistedRuleSets.AddAsync(ruleSet.ToPersistedModel(), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RuleSet ruleSet, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuleSets.FirstAsync(x => x.Id == ruleSet.Id, cancellationToken);
        persisted.Slug = ruleSet.Slug;
        persisted.Generation = ruleSet.Generation;
        persisted.Title = ruleSet.Title;
        persisted.Version = ruleSet.Version;
        persisted.Status = ruleSet.Status;
        persisted.Summary = ruleSet.Summary;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuleSets.FirstAsync(x => x.Id == id, cancellationToken);
        context.PersistedRuleSets.Remove(persisted);
        await context.SaveChangesAsync(cancellationToken);
    }
}
