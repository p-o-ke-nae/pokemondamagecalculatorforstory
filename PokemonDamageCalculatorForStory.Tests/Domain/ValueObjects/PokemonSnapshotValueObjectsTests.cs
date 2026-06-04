using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.ValueObjects;

public sealed class PokemonSnapshotValueObjectsTests
{
    [Fact]
    public void SpeciesBaseStats_FromJson_ReturnsCanonicalJson()
    {
        var stats = SpeciesBaseStats.FromJson(
            """
            {
              "Attack": 55,
              "Hp": 35,
              "Defense": 40,
              "SpecialDefense": 50,
              "Speed": 90,
              "SpecialAttack": 50
            }
            """);

        Assert.Equal("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}", stats.ToJson());
    }

    [Fact]
    public void SpeciesBaseStats_FromJson_Throws_WhenValueIsOutsideSupportedRange()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            SpeciesBaseStats.FromJson("{\"Hp\":0,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"));

        Assert.Equal("BaseStats.Hp must be between 1 and 255.", exception.Message);
    }

    [Fact]
    public void IndividualValues_FromJson_ReturnsCanonicalJson()
    {
        var values = IndividualValues.FromJson(
            """
            {
              "Attack": 31,
              "Hp": 30,
              "Defense": 29,
              "SpecialDefense": 27,
              "Speed": 26,
              "SpecialAttack": 28
            }
            """);

        Assert.Equal("{\"Hp\":30,\"Attack\":31,\"Defense\":29,\"SpecialAttack\":28,\"SpecialDefense\":27,\"Speed\":26}", values.ToJson());
    }

    [Fact]
    public void IndividualValues_FromJson_Throws_WhenValueExceedsPerStatLimit()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            IndividualValues.FromJson("{\"Hp\":31,\"Attack\":32,\"Defense\":31,\"SpecialAttack\":31,\"SpecialDefense\":31,\"Speed\":31}"));

        Assert.Equal("IVs.Attack must be between 0 and 31.", exception.Message);
    }

    [Fact]
    public void PokemonStats_FromJson_ReturnsCanonicalJson()
    {
        var stats = PokemonStats.FromJson(
            """
            {
              "Attack": 55,
              "Hp": 35,
              "Defense": 40,
              "SpecialDefense": 50,
              "Speed": 90,
              "SpecialAttack": 50
            }
            """);

        Assert.Equal("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}", stats.ToJson());
    }

    [Fact]
    public void PokemonStats_FromJson_NormalizesCaseInsensitiveFieldNames()
    {
        var stats = PokemonStats.FromJson(
            """
            {
              "hp": 35,
              "attack": 55,
              "defense": 40,
              "specialattack": 50,
              "specialdefense": 50,
              "speed": 90
            }
            """);

        Assert.Equal("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}", stats.ToJson());
    }

    [Fact]
    public void PokemonStats_FromJson_Throws_WhenRequiredFieldIsMissing()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            PokemonStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50}"));

        Assert.Equal("Stats is missing required fields: Speed.", exception.Message);
    }

    [Fact]
    public void PokemonStats_FromJson_Throws_WhenFieldIsUnsupported()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            PokemonStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90,\"Accuracy\":100}"));

        Assert.Equal("Stats contains unsupported field 'Accuracy'.", exception.Message);
    }

    [Fact]
    public void PokemonStats_FromJson_Throws_WhenFieldIsDuplicatedUsingDifferentCase()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            PokemonStats.FromJson("{\"Hp\":35,\"hp\":36,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"));

        Assert.Equal("Stats contains duplicate field 'hp'.", exception.Message);
    }

    [Fact]
    public void PokemonStats_FromJson_Throws_WhenValueIsNotPositive()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            PokemonStats.FromJson("{\"Hp\":35,\"Attack\":0,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"));

        Assert.Equal("Stats.Attack must be greater than zero.", exception.Message);
    }

    [Fact]
    public void EffortValues_FromJson_Throws_WhenValueExceedsPerStatLimit()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            EffortValues.FromJson("{\"Hp\":0,\"Attack\":253,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":0}"));

        Assert.Equal("EVs.Attack must be between 0 and 252.", exception.Message);
    }

    [Fact]
    public void EffortValues_FromJson_Throws_WhenTotalExceedsLimit()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            EffortValues.FromJson("{\"Hp\":252,\"Attack\":252,\"Defense\":6,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":1}"));

        Assert.Equal("EV total must be less than or equal to 510.", exception.Message);
    }
}
