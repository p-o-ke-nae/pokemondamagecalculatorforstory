using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.ValueObjects;

public sealed class StatusAilmentTests
{
    [Fact(DisplayName = "StatusAilment_Constructor_主状態異常と追加状態異常を両立できる")]
    public void StatusAilment_Constructor_AllowsPrimaryAndAdditionalConditionsToCoexist()
    {
        var ailment = new StatusAilment(
            PrimaryStatusAilment.Poison,
            AdditionalBattleCondition.Confusion | AdditionalBattleCondition.LeechSeed);

        Assert.Equal(PrimaryStatusAilment.Poison, ailment.PrimaryAilment);
        Assert.True(ailment.HasPrimaryAilment);
        Assert.True(ailment.HasAnyAilment);
        Assert.True(ailment.HasAdditionalCondition(AdditionalBattleCondition.Confusion));
        Assert.True(ailment.HasAdditionalCondition(AdditionalBattleCondition.LeechSeed));
    }

    [Fact(DisplayName = "StatusAilment_WithPrimaryAilment_既存の主状態異常を置き換える")]
    public void StatusAilment_WithPrimaryAilment_ReplacesExistingPrimaryAilment()
    {
        var ailment = new StatusAilment(PrimaryStatusAilment.Poison, AdditionalBattleCondition.Confusion);

        var updated = ailment.WithPrimaryAilment(PrimaryStatusAilment.Paralysis);

        Assert.Equal(PrimaryStatusAilment.Paralysis, updated.PrimaryAilment);
        Assert.True(updated.HasAdditionalCondition(AdditionalBattleCondition.Confusion));
    }

    [Fact(DisplayName = "StatusAilment_AddAdditionalCondition_主状態異常がある場合も主状態異常を保持する")]
    public void StatusAilment_AddAdditionalCondition_PreservesPrimaryAilmentWhenPrimaryExists()
    {
        var ailment = new StatusAilment(PrimaryStatusAilment.Poison, AdditionalBattleCondition.None);

        var updated = ailment.AddAdditionalCondition(AdditionalBattleCondition.Confusion);

        Assert.Equal(PrimaryStatusAilment.Poison, updated.PrimaryAilment);
        Assert.Equal(AdditionalBattleCondition.Confusion, updated.AdditionalConditions);
    }

    [Fact(DisplayName = "StatusAilment_AddAdditionalCondition_None指定時にArgumentOutOfRangeExceptionをスローする")]
    public void StatusAilment_AddAdditionalCondition_ThrowsArgumentOutOfRangeExceptionWhenConditionIsNone()
    {
        var ailment = StatusAilment.None;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ailment.AddAdditionalCondition(AdditionalBattleCondition.None));

        Assert.Equal("condition", exception.ParamName);
    }

    [Fact(DisplayName = "StatusAilment_Constructor_不正な追加状態異常指定時にArgumentOutOfRangeExceptionをスローする")]
    public void StatusAilment_Constructor_ThrowsArgumentOutOfRangeExceptionWhenAdditionalConditionIsUnknown()
    {
        var invalidCondition = (AdditionalBattleCondition)4;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StatusAilment(PrimaryStatusAilment.None, invalidCondition));

        Assert.Equal("additionalConditions", exception.ParamName);
    }
}