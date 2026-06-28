using PokemonDamageCalculatorForStory.Domain.DamageCalculation;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.Entities;

public sealed class PokemonDamagePolicyBurnModifierTests
{
    public static TheoryData<PrimaryStatusAilment, bool, MoveCategory, int> Gen3AttackOwnerCases => new()
    {
        { PrimaryStatusAilment.None, false, MoveCategory.Physical, 24 },
        { PrimaryStatusAilment.None, false, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.None, true, MoveCategory.Physical, 24 },
        { PrimaryStatusAilment.None, true, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Poison, false, MoveCategory.Physical, 24 },
        { PrimaryStatusAilment.Poison, false, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Poison, true, MoveCategory.Physical, 35 },
        { PrimaryStatusAilment.Poison, true, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.BadlyPoisoned, false, MoveCategory.Physical, 24 },
        { PrimaryStatusAilment.BadlyPoisoned, false, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.BadlyPoisoned, true, MoveCategory.Physical, 35 },
        { PrimaryStatusAilment.BadlyPoisoned, true, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Paralysis, false, MoveCategory.Physical, 24 },
        { PrimaryStatusAilment.Paralysis, false, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Paralysis, true, MoveCategory.Physical, 35 },
        { PrimaryStatusAilment.Paralysis, true, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Burn, false, MoveCategory.Physical, 12 },
        { PrimaryStatusAilment.Burn, false, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Burn, true, MoveCategory.Physical, 35 },
        { PrimaryStatusAilment.Burn, true, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Freeze, false, MoveCategory.Physical, 24 },
        { PrimaryStatusAilment.Freeze, false, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Freeze, true, MoveCategory.Physical, 35 },
        { PrimaryStatusAilment.Freeze, true, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Sleep, false, MoveCategory.Physical, 24 },
        { PrimaryStatusAilment.Sleep, false, MoveCategory.Special, 24 },
        { PrimaryStatusAilment.Sleep, true, MoveCategory.Physical, 35 },
        { PrimaryStatusAilment.Sleep, true, MoveCategory.Special, 24 },
    };

    public static TheoryData<PrimaryStatusAilment, bool, int> FoulPlayCases => new()
    {
        { PrimaryStatusAilment.None, false, 24 },
        { PrimaryStatusAilment.None, true, 24 },
        { PrimaryStatusAilment.Poison, false, 24 },
        { PrimaryStatusAilment.Poison, true, 35 },
        { PrimaryStatusAilment.BadlyPoisoned, false, 24 },
        { PrimaryStatusAilment.BadlyPoisoned, true, 35 },
        { PrimaryStatusAilment.Paralysis, false, 24 },
        { PrimaryStatusAilment.Paralysis, true, 35 },
        { PrimaryStatusAilment.Burn, false, 12 },
        { PrimaryStatusAilment.Burn, true, 35 },
        { PrimaryStatusAilment.Freeze, false, 24 },
        { PrimaryStatusAilment.Freeze, true, 35 },
        { PrimaryStatusAilment.Sleep, false, 24 },
        { PrimaryStatusAilment.Sleep, true, 35 },
    };

    [Theory(DisplayName = "Battle_CalculateDamage_根性とやけど補正を状態異常と技分類に応じて適用する")]
    [MemberData(nameof(Gen3AttackOwnerCases))]
    public void Battle_CalculateDamage_AppliesGutsAndBurnModifiersByPrimaryAilmentAndMoveCategory(
        PrimaryStatusAilment primaryAilment,
        bool hasGuts,
        MoveCategory category,
        int expectedDamage)
    {
        var battle = CreateBattle(new PokemonDamagePolicyGen3());
        var attacker = CreateBattlePokemon(
            attack: 100,
            specialAttack: 100,
            primaryAilment: primaryAilment,
            hasGuts: hasGuts);
        var defender = CreateBattlePokemon(defense: 100, specialDefense: 100);
        var move = CreateMove("test-move", category);

        var result = Calculate(battle, attacker, defender, move);

        Assert.Equal([expectedDamage], result.DamageRolls);
    }

    [Theory(DisplayName = "Battle_CalculateDamage_イカサマでは対象側の根性とやけど状態を使用する")]
    [MemberData(nameof(FoulPlayCases))]
    public void Battle_CalculateDamage_UsesDefenderGutsAndBurnStateForBurnCorrectionWithFoulPlay(
        PrimaryStatusAilment primaryAilment,
        bool hasGuts,
        int expectedDamage)
    {
        var battle = CreateBattle(new PokemonDamagePolicyGen3());
        var attacker = CreateBattlePokemon(attack: 80);
        var defender = CreateBattlePokemon(
            attack: 100,
            defense: 100,
            primaryAilment: primaryAilment,
            hasGuts: hasGuts);
        var move = CreateMove("イカサマ", MoveCategory.Physical, [new UseTargetAttackEffect()]);

        var result = Calculate(battle, attacker, defender, move);

        Assert.Equal([expectedDamage], result.DamageRolls);
    }

    private static DamageResult Calculate(
        Battle battle,
        BattlePokemon attacker,
        BattlePokemon defender,
        Move move)
    {
        return battle.CalculateDamage(attacker, defender, move);
    }

    private static Battle CreateBattle(IPokemonDamagePolicy damagePolicy)
    {
        return new Battle(BattleFormat.Single, new Field(Weather.None, Terrain.None, new ScreenState()), damagePolicy);
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

    private static Move CreateMove(string name, MoveCategory category, IMoveDamageEffect[]? damageEffects = null)
    {
        return new Move(
            name,
            PokemonType.Normal,
            category,
            new Power(50),
            TargetScope.SingleOpponent,
            damageEffects ?? [],
            []);
    }
}