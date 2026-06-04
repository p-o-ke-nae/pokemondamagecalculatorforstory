using PokemonDamageCalculatorForStory.Application.UseCases.Commands;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Application.UseCases.Commands;

public sealed class CalculateDamageCommandHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Exactly16DamageRolls()
    {
        var repository = new StubBattleRepository();
        var handler = new CalculateDamageCommandHandler(repository);
        var command = new CalculateDamageCommand(Guid.NewGuid(), Guid.NewGuid(), 50, 100, 80, false, false, 100, 1.0f);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(16, result.DamageRolls.Count);
    }

    [Fact]
    public async Task Handle_FirstRollIsMinimum_LastRollIsMaximum()
    {
        var repository = new StubBattleRepository();
        var handler = new CalculateDamageCommandHandler(repository);
        var command = new CalculateDamageCommand(Guid.NewGuid(), Guid.NewGuid(), 50, 100, 80, false, false, 100, 1.0f);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(result.DamageRolls.Min(), result.DamageRolls[0]);
        Assert.Equal(result.DamageRolls.Max(), result.DamageRolls[^1]);
    }

    [Fact]
    public async Task Handle_DeterministicInput_MatchesHandCalculatedValues()
    {
        var repository = new StubBattleRepository();
        var handler = new CalculateDamageCommandHandler(repository);
        var command = new CalculateDamageCommand(Guid.NewGuid(), Guid.NewGuid(), 50, 100, 80, false, false, 100, 1.0f);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(31, result.DamageRolls[0]);
        Assert.Equal(37, result.DamageRolls[15]);
    }

    private sealed class StubBattleRepository : IBattleRepository
    {
        public Task<IReadOnlyList<Battle>> ListByRunAsync(Guid runId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Battle>>(Array.Empty<Battle>());

        public Task<Battle?> FindAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default)
            => Task.FromResult<Battle?>(null);

        public Task<Battle> SaveAsync(Battle battle, CancellationToken cancellationToken = default)
            => Task.FromResult(battle);

        public Task<bool> DeleteAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<OwnPokemonSnapshot>> ListSnapshotsByRunAsync(Guid runId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OwnPokemonSnapshot>>(Array.Empty<OwnPokemonSnapshot>());

        public Task<OwnPokemonSnapshot> SaveSnapshotAsync(OwnPokemonSnapshot snapshot, Guid runId, CancellationToken cancellationToken = default)
            => Task.FromResult(snapshot);

        public Task<CalculationResult> SaveCalculationResultAsync(CalculationResult result, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
