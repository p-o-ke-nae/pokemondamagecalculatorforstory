using PokemonDamageCalculatorForStory.Domain.DamageCalculation;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.DamageCalculation.AbilityEffects;

public sealed class GutsAbilityEffectTests
{
    public static TheoryData<PrimaryStatusAilment, int> AttackSourceCases => new()
    {
        { PrimaryStatusAilment.None, 100 },
        { PrimaryStatusAilment.Poison, 150 },
        { PrimaryStatusAilment.BadlyPoisoned, 150 },
        { PrimaryStatusAilment.Paralysis, 150 },
        { PrimaryStatusAilment.Burn, 150 },
        { PrimaryStatusAilment.Freeze, 150 },
        { PrimaryStatusAilment.Sleep, 150 },
    };

    public static TheoryData<PrimaryStatusAilment, int> NonAttackSourceCases => new()
    {
        { PrimaryStatusAilment.None, 100 },
        { PrimaryStatusAilment.Poison, 100 },
        { PrimaryStatusAilment.BadlyPoisoned, 100 },
        { PrimaryStatusAilment.Paralysis, 100 },
        { PrimaryStatusAilment.Burn, 100 },
        { PrimaryStatusAilment.Freeze, 100 },
        { PrimaryStatusAilment.Sleep, 100 },
    };

    public static TheoryData<PrimaryStatusAilment> PrimaryAilmentAttackCases => new()
    {
        { PrimaryStatusAilment.Poison },
        { PrimaryStatusAilment.BadlyPoisoned },
        { PrimaryStatusAilment.Paralysis },
        { PrimaryStatusAilment.Burn },
        { PrimaryStatusAilment.Freeze },
        { PrimaryStatusAilment.Sleep },
    };

    public static TheoryData<PrimaryStatusAilment> NonBurnPenaltyCases => new()
    {
        { PrimaryStatusAilment.None },
        { PrimaryStatusAilment.Poison },
        { PrimaryStatusAilment.BadlyPoisoned },
        { PrimaryStatusAilment.Paralysis },
        { PrimaryStatusAilment.Freeze },
        { PrimaryStatusAilment.Sleep },
    };

    [Theory(DisplayName = "GutsAbilityEffect_GetAttackStatModifier_Attack参照では主要状態異常に応じてこうげき補正を返す")]
    [MemberData(nameof(AttackSourceCases))]
    public void GutsAbilityEffect_GetAttackStatModifier_ReturnsAttackModifierForAttackSource(
        PrimaryStatusAilment primaryAilment,
        int expectedAttack)
    {
        var effect = new GutsAbilityEffect();
        var subject = CreateBattlePokemon(primaryAilment);
        var spec = CreateDamageSpec(StatSelector.Attack);
        var context = CreateDamageContext(subject);

        var modifier = effect.GetAttackStatModifier(subject, spec, context);

        Assert.Equal(expectedAttack, modifier.ApplyTo(100));
    }

    [Theory(DisplayName = "GutsAbilityEffect_GetAttackStatModifier_Attack以外の参照ではこうげき補正を返さない")]
    [MemberData(nameof(NonAttackSourceCases))]
    public void GutsAbilityEffect_GetAttackStatModifier_DoesNotReturnAttackModifierForNonAttackSource(
        PrimaryStatusAilment primaryAilment,
        int expectedAttack)
    {
        var effect = new GutsAbilityEffect();
        var subject = CreateBattlePokemon(primaryAilment);
        var spec = CreateDamageSpec(StatSelector.SpecialAttack);
        var context = CreateDamageContext(subject);

        var modifier = effect.GetAttackStatModifier(subject, spec, context);

        Assert.Equal(expectedAttack, modifier.ApplyTo(100));
    }

    [Theory(DisplayName = "GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_主要状態異常があるAttack参照ではやけど半減を無効化する")]
    [MemberData(nameof(PrimaryAilmentAttackCases))]
    public void GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_IgnoresBurnPenaltyForPrimaryAilmentAttackSource(
        PrimaryStatusAilment subjectPrimaryAilment)
    {
        var effect = new GutsAbilityEffect();
        var subject = CreateBattlePokemon(subjectPrimaryAilment);
        var spec = CreateDamageSpec(StatSelector.Attack);
        var context = CreateDamageContext(subject);

        var actual = effect.IgnoresPrimaryStatusAilmentPenalty(
            subject,
            PrimaryStatusAilment.Burn,
            spec,
            context);

        Assert.True(actual);
    }

    [Fact(DisplayName = "GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_主要状態異常がない場合はやけど半減を無効化しない")]
    public void GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_DoesNotIgnoreBurnPenaltyWithoutPrimaryAilment()
    {
        var effect = new GutsAbilityEffect();
        var subject = CreateBattlePokemon(PrimaryStatusAilment.None);
        var spec = CreateDamageSpec(StatSelector.Attack);
        var context = CreateDamageContext(subject);

        var actual = effect.IgnoresPrimaryStatusAilmentPenalty(
            subject,
            PrimaryStatusAilment.Burn,
            spec,
            context);

        Assert.False(actual);
    }

    [Theory(DisplayName = "GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_Attack以外の参照ではやけど半減を無効化しない")]
    [MemberData(nameof(PrimaryAilmentAttackCases))]
    public void GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_DoesNotIgnoreBurnPenaltyForNonAttackSource(
        PrimaryStatusAilment subjectPrimaryAilment)
    {
        var effect = new GutsAbilityEffect();
        var subject = CreateBattlePokemon(subjectPrimaryAilment);
        var spec = CreateDamageSpec(StatSelector.SpecialAttack);
        var context = CreateDamageContext(subject);

        var actual = effect.IgnoresPrimaryStatusAilmentPenalty(
            subject,
            PrimaryStatusAilment.Burn,
            spec,
            context);

        Assert.False(actual);
    }

    [Theory(DisplayName = "GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_やけど以外の状態異常ペナルティは無効化しない")]
    [MemberData(nameof(NonBurnPenaltyCases))]
    public void GutsAbilityEffect_IgnoresPrimaryStatusAilmentPenalty_DoesNotIgnoreNonBurnPenalty(
        PrimaryStatusAilment penaltyPrimaryAilment)
    {
        var effect = new GutsAbilityEffect();
        var subject = CreateBattlePokemon(PrimaryStatusAilment.Burn);
        var spec = CreateDamageSpec(StatSelector.Attack);
        var context = CreateDamageContext(subject);

        var actual = effect.IgnoresPrimaryStatusAilmentPenalty(
            subject,
            penaltyPrimaryAilment,
            spec,
            context);

        Assert.False(actual);
    }

    private static DamageSpec CreateDamageSpec(StatSelector attackSource)
    {
        return new DamageSpec(
            attackSource,
            StatSelector.Defense,
            new Power(50),
            fixedDamage: 0);
    }

    private static DamageContext CreateDamageContext(BattlePokemon attacker)
    {
        var defender = CreateBattlePokemon(PrimaryStatusAilment.None);
        return new DamageContext(attacker, defender, MoveCategory.Physical, new MoveTargetCount(1));
    }

    private static BattlePokemon CreateBattlePokemon(PrimaryStatusAilment primaryAilment)
    {
        return new BattlePokemon(
            new Level(50),
            new PokemonTyping([PokemonType.Normal]),
            new PokemonStats(100, 100, 100, 100, 100, 100),
            new StatStages(),
            new Ability("こんじょう"),
            new Item("なし"),
            new StatusAilment(primaryAilment, AdditionalBattleCondition.None));
    }
}