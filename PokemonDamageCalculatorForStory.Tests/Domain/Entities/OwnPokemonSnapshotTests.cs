using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
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
            OwnPokemonSnapshot.Create(battleId, "Pikachu", level, "HP:35", "Speed:252"));

        Assert.Equal("Level must be between 1 and 100.", exception.Message);
    }
}
