using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

public interface IRunRepository
{
    Task<IReadOnlyList<Run>> FindAllAsync(CancellationToken cancellationToken = default);
    Task<Run?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Run> SaveAsync(Run run, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
