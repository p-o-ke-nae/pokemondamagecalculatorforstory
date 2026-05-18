using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

public interface IRuleSetRepository
{
    Task<IReadOnlyList<RuleSet>> FindAllAsync(CancellationToken cancellationToken = default);
    Task<RuleSet?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
