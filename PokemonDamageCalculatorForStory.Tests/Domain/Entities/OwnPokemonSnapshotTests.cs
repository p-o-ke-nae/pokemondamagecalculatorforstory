using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.Entities;

public sealed class OwnPokemonSnapshotTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_Throws_WhenLevelIsOutsideSupportedRange(int level)
    {
        var battleId = Guid.NewGuid();

        var exception = Assert.Throws<ValidationException>(() =>
            OwnPokemonSnapshot.Create(
                battleId,
                "Pikachu",
                level,
                PokemonStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"),
                EffortValues.FromJson("{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":252}")));

        Assert.Equal("Level must be between 1 and 100.", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenSpeciesExceedsMaxLength()
    {
        var battleId = Guid.NewGuid();
        var species = new string('P', OwnPokemonSnapshot.SpeciesMaxLength + 1);

        var exception = Assert.Throws<ValidationException>(() =>
            OwnPokemonSnapshot.Create(
                battleId,
                species,
                50,
                PokemonStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"),
                EffortValues.FromJson("{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":252}")));

        Assert.Equal("Species must be 100 characters or fewer.", exception.Message);
    }
}
