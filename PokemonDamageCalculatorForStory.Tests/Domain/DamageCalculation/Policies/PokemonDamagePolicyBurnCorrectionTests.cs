using PokemonDamageCalculatorForStory.Domain.DamageCalculation;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.BattleEffects;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.DamageCalculation.Policies;

public sealed class PokemonDamagePolicyBurnCorrectionTests
{
    [Fact(DisplayName = "Battle_CalculateDamage_第3世代ではやけど状態の物理技にやけど補正を適用する")]
    public void Battle_CalculateDamage_AppliesBurnCorrectionForBurnedPhysicalMoveInGen3()
    {
        var battle = CreateBattle(new PokemonDamagePolicyGen3());
        var attacker = CreateBattlePokemon(attack: 100, abilityName: "もうか", primaryAilment: PrimaryStatusAilment.Burn);
        var defender = CreateBattlePokemon(defense: 100);
        var move = CreateMove("たいあたり", MoveCategory.Physical);

        var result = Calculate(battle, attacker, defender, move);

        Assert.Equal([12], result.DamageRolls);
    }

    [Fact(DisplayName = "Battle_CalculateDamage_やけど状態でも特殊技にはやけど補正を適用しない")]
    public void Battle_CalculateDamage_DoesNotApplyBurnCorrectionToSpecialMoveWhenBurned()
    {
        var battle = CreateBattle(new PokemonDamagePolicyGen3());
        var attacker = CreateBattlePokemon(attack: 100, specialAttack: 100, abilityName: "もうか", primaryAilment: PrimaryStatusAilment.Burn);
        var defender = CreateBattlePokemon(defense: 100, specialDefense: 100);
        var move = CreateMove("みずでっぽう", MoveCategory.Special);

        var result = Calculate(battle, attacker, defender, move);

        Assert.Equal([24], result.DamageRolls);
    }

    [Fact(DisplayName = "Battle_CalculateDamage_イカサマでは対象のやけど状態をやけど補正に使用する")]
    public void Battle_CalculateDamage_UsesDefenderBurnStateForBurnCorrectionWithFoulPlay()
    {
        var battle = CreateBattle(new PokemonDamagePolicyGen3());
        var attacker = CreateBattlePokemon(attack: 80, abilityName: "もうか");
        var defender = CreateBattlePokemon(attack: 100, defense: 100, abilityName: "もうか", primaryAilment: PrimaryStatusAilment.Burn);
        var move = CreateMove("イカサマ", MoveCategory.Physical, [new UseTargetAttackEffect()]);

        var result = Calculate(battle, attacker, defender, move);

        Assert.Equal([12], result.DamageRolls);
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
        string abilityName = "もうか",
        PrimaryStatusAilment primaryAilment = PrimaryStatusAilment.None)
    {
        return new BattlePokemon(
            new Level(50),
            new PokemonTyping([PokemonType.Normal]),
            new PokemonStats(100, attack, defense, specialAttack, specialDefense, 100),
            new StatStages(),
            new Ability(abilityName),
            new Item("なし"),
            new StatusAilment(primaryAilment, AdditionalBattleCondition.None));
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