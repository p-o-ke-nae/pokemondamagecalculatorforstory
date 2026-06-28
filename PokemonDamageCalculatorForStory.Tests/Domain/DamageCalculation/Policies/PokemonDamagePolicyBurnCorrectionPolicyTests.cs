using PokemonDamageCalculatorForStory.Domain.DamageCalculation;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.DamageCalculation.Policies;

public sealed class PokemonDamagePolicyBurnCorrectionPolicyTests
{
    [Fact(DisplayName = "PokemonDamagePolicyGen3_Calculate_やけど状態の物理技にやけど補正を適用する")]
    public void PokemonDamagePolicyGen3_Calculate_AppliesBurnCorrectionForBurnedPhysicalMove()
    {
        var policy = new PokemonDamagePolicyGen3();
        var attacker = CreateBattlePokemon(attack: 100, primaryAilment: PrimaryStatusAilment.Burn);
        var defender = CreateBattlePokemon(defense: 100);

        var result = Calculate(policy, attacker, defender, MoveCategory.Physical);

        Assert.Equal([12], result.DamageRolls);
    }

    [Fact(DisplayName = "PokemonDamagePolicyGen3_Calculate_やけど状態でも特殊技にはやけど補正を適用しない")]
    public void PokemonDamagePolicyGen3_Calculate_DoesNotApplyBurnCorrectionToSpecialMoveWhenBurned()
    {
        var policy = new PokemonDamagePolicyGen3();
        var attacker = CreateBattlePokemon(attack: 100, specialAttack: 100, primaryAilment: PrimaryStatusAilment.Burn);
        var defender = CreateBattlePokemon(defense: 100, specialDefense: 100);

        var result = Calculate(policy, attacker, defender, MoveCategory.Special);

        Assert.Equal([24], result.DamageRolls);
    }

    [Fact(DisplayName = "PokemonDamagePolicyGen3_Calculate_対象の攻撃値を参照する場合は対象のやけど状態をやけど補正に使用する")]
    public void PokemonDamagePolicyGen3_Calculate_UsesDefenderBurnStateWhenAttackStatOwnerIsDefender()
    {
        var policy = new PokemonDamagePolicyGen3();
        var attacker = CreateBattlePokemon(attack: 80);
        var defender = CreateBattlePokemon(attack: 100, defense: 100, primaryAilment: PrimaryStatusAilment.Burn);

        var result = Calculate(policy, attacker, defender, MoveCategory.Physical, DamageStatOwner.Defender);

        Assert.Equal([12], result.DamageRolls);
    }

    private static DamageResult Calculate(
        IPokemonDamagePolicy policy,
        BattlePokemon attacker,
        BattlePokemon defender,
        MoveCategory category,
        DamageStatOwner attackStatOwner = DamageStatOwner.Attacker)
    {
        var move = new Move(
            "test-move",
            PokemonType.Normal,
            category,
            new Power(50),
            TargetScope.SingleOpponent,
            [],
            []);
        var spec = new DamageSpec(
            StatSelector.Attack,
            attackStatOwner,
            StatSelector.Defense,
            move.Power,
            fixedDamage: 0);
        var context = new DamageContext(attacker, defender, move, new MoveTargetCount(1));

        return policy.Calculate(spec, context);
    }

    private static BattlePokemon CreateBattlePokemon(
        int attack = 100,
        int defense = 100,
        int specialAttack = 100,
        int specialDefense = 100,
        PrimaryStatusAilment primaryAilment = PrimaryStatusAilment.None)
    {
        return new BattlePokemon(
            new Level(50),
            new PokemonTyping([PokemonType.Normal]),
            new PokemonStats(100, attack, defense, specialAttack, specialDefense, 100),
            new StatStages(),
            new Ability("もうか"),
            new Item("なし"),
            new StatusAilment(primaryAilment, AdditionalBattleCondition.None));
    }
}