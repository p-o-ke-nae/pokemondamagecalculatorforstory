using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Modifiers;
using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;

/// <summary>
/// 特性による攻撃値補正効果を表す。
/// </summary>
public interface IAbilityAttackStatEffect
{
    /// <summary>
    /// 攻撃値補正を取得する。
    /// </summary>
    AttackStatModifier GetAttackStatModifier(
        BattlePokemon subject,
        DamageSpec spec,
        DamageContext context);
}