using PokemonDamageCalculatorForStory.Domain.DamageCalculation;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.DamageCalculation.Policies;

public sealed class PokemonDamagePolicyBurnModifierPolicyTests
{
    public static TheoryData<PrimaryStatusAilment> Gen3PhysicalAttackOwnerCases => new()
    {
        { PrimaryStatusAilment.Poison },
        { PrimaryStatusAilment.BadlyPoisoned },
        { PrimaryStatusAilment.Paralysis },
        { PrimaryStatusAilment.Burn },
        { PrimaryStatusAilment.Freeze },
        { PrimaryStatusAilment.Sleep },
    };

    public static TheoryData<PrimaryStatusAilment> Gen3DoesNotApplyGutsToPhysicalAttackOwnerCases => new()
    {
        { PrimaryStatusAilment.None },
    };

    public static TheoryData<PrimaryStatusAilment, bool> Gen3SpecialAttackCases => new()
    {
        { PrimaryStatusAilment.None, false },
        { PrimaryStatusAilment.None, true },
        { PrimaryStatusAilment.Poison, false },
        { PrimaryStatusAilment.Poison, true },
        { PrimaryStatusAilment.BadlyPoisoned, false },
        { PrimaryStatusAilment.BadlyPoisoned, true },
        { PrimaryStatusAilment.Paralysis, false },
        { PrimaryStatusAilment.Paralysis, true },
        { PrimaryStatusAilment.Burn, false },
        { PrimaryStatusAilment.Burn, true },
        { PrimaryStatusAilment.Freeze, false },
        { PrimaryStatusAilment.Freeze, true },
        { PrimaryStatusAilment.Sleep, false },
        { PrimaryStatusAilment.Sleep, true },
    };

    public static TheoryData<PrimaryStatusAilment, bool, int> Gen3DefenderAttackOwnerCases => new()
    {
        { PrimaryStatusAilment.None, false, 28 },
        { PrimaryStatusAilment.None, true, 28 },
        { PrimaryStatusAilment.Poison, false, 28 },
        { PrimaryStatusAilment.Poison, true, 41 },
        { PrimaryStatusAilment.BadlyPoisoned, false, 28 },
        { PrimaryStatusAilment.BadlyPoisoned, true, 41 },
        { PrimaryStatusAilment.Paralysis, false, 28 },
        { PrimaryStatusAilment.Paralysis, true, 41 },
        { PrimaryStatusAilment.Burn, false, 14 },
        { PrimaryStatusAilment.Burn, true, 41 },
        { PrimaryStatusAilment.Freeze, false, 28 },
        { PrimaryStatusAilment.Freeze, true, 41 },
        { PrimaryStatusAilment.Sleep, false, 28 },
        { PrimaryStatusAilment.Sleep, true, 41 },
    };

    [Theory(DisplayName = "PokemonDamagePolicyGen3_Calculate_状態異常の際物理技に根性が適用された上でやけど補正は無効となる")]
    [MemberData(nameof(Gen3PhysicalAttackOwnerCases))]
    public void PokemonDamagePolicyGen3_Calculate_AppliesGutsAndBurnModifiersToPhysicalMoves(
        PrimaryStatusAilment primaryAilment)
    {
        var boostDamage = 35;
        var policy = new PokemonDamagePolicyGen3();
        var attacker = CreateBattlePokemon(
            attack: 100,
            specialAttack: 100,
            primaryAilment: primaryAilment,
            hasGuts: true);
        var defender = CreateBattlePokemon(defense: 100, specialDefense: 100);

        var result = Calculate(policy, attacker, defender);

        Assert.Equal([boostDamage], result.DamageRolls);
    }

    [Theory(DisplayName = "PokemonDamagePolicyGen3_Calculate_状態異常でない場合根性は適用されない")]
    [MemberData(nameof(Gen3DoesNotApplyGutsToPhysicalAttackOwnerCases))]
    public void PokemonDamagePolicyGen3_Calculate_DoesNotApplyGutsToPhysicalMoves(
        PrimaryStatusAilment primaryAilment)
    {
        var normalDamage = 24;
        var policy = new PokemonDamagePolicyGen3();
        var attacker = CreateBattlePokemon(
            attack: 100,
            specialAttack: 100,
            primaryAilment: primaryAilment,
            hasGuts: true);
        var defender = CreateBattlePokemon(defense: 100, specialDefense: 100);

        var result = Calculate(policy, attacker, defender);

        Assert.Equal([normalDamage], result.DamageRolls);
    }

    [Theory(DisplayName = "PokemonDamagePolicyGen3_Calculate_根性とやけど補正は特殊技には適用されない")]
    [MemberData(nameof(Gen3SpecialAttackCases))]
    public void PokemonDamagePolicyGen3_Calculate_DoesNotApplyGutsAndBurnModifiersToSpecialMoves(
        PrimaryStatusAilment primaryAilment,
        bool hasGuts)
    {
        var normalDamage = 24;
        var policy = new PokemonDamagePolicyGen3();
        var attacker = CreateBattlePokemon(
            attack: 100,
            specialAttack: 100,
            primaryAilment: primaryAilment,
            hasGuts: hasGuts);
        var defender = CreateBattlePokemon(defense: 100, specialDefense: 100);

        var result = Calculate(policy, attacker, defender, category: MoveCategory.Special);

        Assert.Equal([normalDamage], result.DamageRolls);
    }

    [Theory(DisplayName = "PokemonDamagePolicyGen3_Calculate_対象の攻撃値を参照する場合は対象側の根性とやけど状態を使用する")]
    [MemberData(nameof(Gen3DefenderAttackOwnerCases))]
    public void PokemonDamagePolicyGen3_Calculate_UsesDefenderGutsAndBurnStateWhenAttackStatOwnerIsDefender(
        PrimaryStatusAilment primaryAilment,
        bool hasGuts,
        int expectedDamage)
    {
        var policy = new PokemonDamagePolicyGen3();
        var attacker = CreateBattlePokemon(attack: 80);
        var defender = CreateBattlePokemon(
            attack: 120,
            defense: 100,
            primaryAilment: primaryAilment,
            hasGuts: hasGuts);

        // 相手ポケモンの攻撃値を参照して技を使用する
        var result = Calculate(policy, attacker, defender, attackStatOwner: DamageStatOwner.Defender);

        Assert.Equal([expectedDamage], result.DamageRolls);
    }

    private static DamageResult Calculate(
        IPokemonDamagePolicy policy,
        BattlePokemon attacker,
        BattlePokemon defender,
        MoveCategory category = MoveCategory.Physical,
        DamageStatOwner attackStatOwner = DamageStatOwner.Attacker)
    {
        StatSelector statSelector = MoveCategory.Physical == category ? StatSelector.Attack : StatSelector.SpecialAttack;
        var spec = new DamageSpec(
                statSelector,
                attackStatOwner,
                StatSelector.Defense,
                new Power(50),
                fixedDamage: 0);
        var context = new DamageContext(attacker, defender, category, new MoveTargetCount(1));

        return policy.Calculate(spec, context);
    }

    private static BattlePokemon CreateBattlePokemon(
        int attack = 100,
        int defense = 100,
        int specialAttack = 100,
        int specialDefense = 100,
        PrimaryStatusAilment primaryAilment = PrimaryStatusAilment.None,
        bool hasGuts = false)
    {
        return new BattlePokemon(
            new Level(50),
            new PokemonTyping([PokemonType.Normal]),
            new PokemonStats(100, attack, defense, specialAttack, specialDefense, 100),
            new StatStages(),
            CreateAbility(hasGuts),
            new Item("なし"),
            new StatusAilment(primaryAilment, AdditionalBattleCondition.None));
    }

    private static Ability CreateAbility(bool hasGuts)
    {
        if (!hasGuts)
        {
            return new Ability("もうか");
        }

        var effect = new GutsAbilityEffect();
        return new Ability("こんじょう", statusAilmentEffects: [effect], attackStatEffects: [effect]);
    }
}