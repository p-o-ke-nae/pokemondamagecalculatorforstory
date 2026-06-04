using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;
using PokemonDamageCalculatorForStory.Infrastructure.Mappers;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Infrastructure.Mappers;

public sealed class PersistedBattleMapperTests
{
    [Fact]
    public void ToDomainSnapshot_PreservesUnknownLegacyBaseStatsAndIVs()
    {
        var persisted = new PersistedOwnPokemonSnapshot
        {
            Id = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            BattleId = Guid.NewGuid(),
            Species = "Pikachu",
            Level = 50,
            BaseStats = null,
            IVs = null,
            Stats = "{\"Hp\":120,\"Attack\":75,\"Defense\":60,\"SpecialAttack\":70,\"SpecialDefense\":70,\"Speed\":110}",
            EVs = "{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":252}"
        };

        var snapshot = persisted.ToDomainSnapshot();

        Assert.Null(snapshot.BaseStats);
        Assert.Null(snapshot.IVs);
        Assert.Equal(120, snapshot.Stats.Hp.Value);
        Assert.Equal(252, snapshot.EVs.Speed.Value);
    }
}
