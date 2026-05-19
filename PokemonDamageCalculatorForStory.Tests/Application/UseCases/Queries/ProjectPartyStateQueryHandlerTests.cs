using PokemonDamageCalculatorForStory.Application.UseCases.Queries;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Application.UseCases.Queries;

public sealed class ProjectPartyStateQueryHandlerTests
{
    [Fact]
    public async Task Handle_ProjectsUnknownLegacyBaseStatsAndIVsAsNull()
    {
        var runId = Guid.NewGuid();
        IReadOnlyList<OwnPokemonSnapshot> snapshots =
        [
            OwnPokemonSnapshot.Restore(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Pikachu",
                50,
                null,
                null,
                PokemonStats.FromJson("{\"Hp\":120,\"Attack\":75,\"Defense\":60,\"SpecialAttack\":70,\"SpecialDefense\":70,\"Speed\":110}"),
                EffortValues.FromJson("{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":252}"))
        ];

        var handler = new ProjectPartyStateQueryHandler(new StubBattleRepository(snapshots));

        var result = await handler.Handle(new ProjectPartyStateQuery(runId), CancellationToken.None);

        var snapshot = Assert.Single(result.Pokemon);
        Assert.Null(snapshot.BaseStats);
        Assert.Null(snapshot.IVs);
        Assert.Equal("{\"Hp\":120,\"Attack\":75,\"Defense\":60,\"SpecialAttack\":70,\"SpecialDefense\":70,\"Speed\":110}", snapshot.Stats);
    }

    private sealed class StubBattleRepository(IReadOnlyList<OwnPokemonSnapshot> snapshots) : IBattleRepository
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
            => Task.FromResult(snapshots);

        public Task<OwnPokemonSnapshot> SaveSnapshotAsync(OwnPokemonSnapshot snapshot, Guid runId, CancellationToken cancellationToken = default)
            => Task.FromResult(snapshot);

        public Task<CalculationResult> SaveCalculationResultAsync(CalculationResult result, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
