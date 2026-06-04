using PokemonDamageCalculatorForStory.Domain.Calculations;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.Calculations;

public sealed class PokemonDamageFormulaTests
{
    [Fact]
    public void CalculateRolls_ReturnsExpectedDamageRolls()
    {
        var result = PokemonDamageFormula.CalculateRolls(
            PokemonLevel.Create(50),
            AttackStat.Create(100),
            MovePower.Create(80),
            hasStab: false,
            DefenseStat.Create(100),
            TypeEffectivenessMultiplier.Create(1.0f));

        Assert.Equal(16, result.Count);
        Assert.Equal(31, result[0]);
        Assert.Equal(37, result[^1]);
    }
}
