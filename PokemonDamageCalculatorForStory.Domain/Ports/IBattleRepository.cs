using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

public interface IBattleRepository
{
    Task<IReadOnlyList<Battle>> ListByRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<Battle?> FindAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default);
    Task<Battle> SaveAsync(Battle battle, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnPokemonSnapshot>> ListSnapshotsByRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<OwnPokemonSnapshot> SaveSnapshotAsync(OwnPokemonSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<CalculationResult> SaveCalculationResultAsync(CalculationResult result, CancellationToken cancellationToken = default);
}
