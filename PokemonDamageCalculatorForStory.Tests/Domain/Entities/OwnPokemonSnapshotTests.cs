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
                SpeciesBaseStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"),
                IndividualValues.FromJson("{\"Hp\":31,\"Attack\":31,\"Defense\":31,\"SpecialAttack\":31,\"SpecialDefense\":31,\"Speed\":31}"),
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
                SpeciesBaseStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"),
                IndividualValues.FromJson("{\"Hp\":31,\"Attack\":31,\"Defense\":31,\"SpecialAttack\":31,\"SpecialDefense\":31,\"Speed\":31}"),
                PokemonStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"),
                EffortValues.FromJson("{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":252}")));

        Assert.Equal("Species must be 100 characters or fewer.", exception.Message);
    }

    [Fact]
    public void Create_AssignsBaseStatsAndIVs()
    {
        var snapshot = OwnPokemonSnapshot.Create(
            Guid.NewGuid(),
            "Pikachu",
            50,
            SpeciesBaseStats.FromJson("{\"Hp\":35,\"Attack\":55,\"Defense\":40,\"SpecialAttack\":50,\"SpecialDefense\":50,\"Speed\":90}"),
            IndividualValues.FromJson("{\"Hp\":31,\"Attack\":30,\"Defense\":29,\"SpecialAttack\":28,\"SpecialDefense\":27,\"Speed\":26}"),
            PokemonStats.FromJson("{\"Hp\":120,\"Attack\":75,\"Defense\":60,\"SpecialAttack\":70,\"SpecialDefense\":70,\"Speed\":110}"),
            EffortValues.FromJson("{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":252}"));

        Assert.Equal(35, snapshot.BaseStats!.Value.Hp.Value);
        Assert.Equal(30, snapshot.IVs!.Value.Attack.Value);
    }

    [Fact]
    public void Restore_AllowsUnknownLegacyBaseStatsAndIVs()
    {
        var snapshot = OwnPokemonSnapshot.Restore(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pikachu",
            50,
            null,
            null,
            PokemonStats.FromJson("{\"Hp\":120,\"Attack\":75,\"Defense\":60,\"SpecialAttack\":70,\"SpecialDefense\":70,\"Speed\":110}"),
            EffortValues.FromJson("{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":252}"));

        Assert.Null(snapshot.BaseStats);
        Assert.Null(snapshot.IVs);
    }
}
