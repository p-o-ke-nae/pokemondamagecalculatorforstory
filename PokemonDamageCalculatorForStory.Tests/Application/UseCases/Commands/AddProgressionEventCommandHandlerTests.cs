using PokemonDamageCalculatorForStory.Application.UseCases.Commands;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Application.UseCases.Commands;

public sealed class AddProgressionEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_ParsesSnapshotPayloads_AndReturnsCanonicalJsonStrings()
    {
        var runId = Guid.NewGuid();
        var battleId = Guid.NewGuid();
        var repository = new StubBattleRepository(Battle.Restore(battleId, runId, "Geodude", 1));
        var handler = new AddProgressionEventCommandHandler(repository);
        var command = new AddProgressionEventCommand(
            runId,
            battleId,
            "Pikachu",
            20,
            """
            {
              "Attack": 55,
              "Hp": 35,
              "Defense": 40,
              "SpecialDefense": 50,
              "Speed": 90,
              "SpecialAttack": 50
            }
            """,
            """
            {
              "Attack": 252,
              "Hp": 0,
              "Defense": 0,
              "SpecialDefense": 4,
              "Speed": 252,
              "SpecialAttack": 0
            }
            """);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}", result.Stats);
        Assert.Equal("{\"Hp\":0,\"Attack\":252,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":4,\"Speed\":252}", result.EVs);
        Assert.NotNull(repository.SavedSnapshot);
        Assert.Equal(55, repository.SavedSnapshot!.Stats.Attack.Value);
        Assert.Equal(252, repository.SavedSnapshot.EVs.Speed.Value);
    }

    private sealed class StubBattleRepository(Battle battle) : IBattleRepository
    {
        public OwnPokemonSnapshot? SavedSnapshot { get; private set; }

        public Task<IReadOnlyList<Battle>> ListByRunAsync(Guid runId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Battle>>(Array.Empty<Battle>());

        public Task<Battle?> FindAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default)
            => Task.FromResult<Battle?>(battle.Id == battleId && battle.RunId == runId ? battle : null);

        public Task<Battle> SaveAsync(Battle battle, CancellationToken cancellationToken = default)
            => Task.FromResult(battle);

        public Task<bool> DeleteAsync(Guid runId, Guid battleId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<OwnPokemonSnapshot>> ListSnapshotsByRunAsync(Guid runId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OwnPokemonSnapshot>>(Array.Empty<OwnPokemonSnapshot>());

        public Task<OwnPokemonSnapshot> SaveSnapshotAsync(OwnPokemonSnapshot snapshot, Guid runId, CancellationToken cancellationToken = default)
        {
            SavedSnapshot = snapshot;
            return Task.FromResult(snapshot);
        }

        public Task<CalculationResult> SaveCalculationResultAsync(CalculationResult result, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
