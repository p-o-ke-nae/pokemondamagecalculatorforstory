using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

public interface IRuleSetRepository
{
    Task<IReadOnlyList<RuleSet>> FindAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuleSet>> FindAllActiveAsync(CancellationToken cancellationToken = default);
    Task<RuleSet?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RuleSet?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> IsReferencedByRunsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> FindReferencedRuleSetIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RuleSet ruleSet, CancellationToken cancellationToken = default);
    Task UpdateAsync(RuleSet ruleSet, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
