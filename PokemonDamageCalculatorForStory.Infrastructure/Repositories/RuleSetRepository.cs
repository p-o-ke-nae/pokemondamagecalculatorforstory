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

    public async Task<RuleSet?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var persisted = await context.PersistedRuleSets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return persisted?.ToDomainEntity();
    }
}
