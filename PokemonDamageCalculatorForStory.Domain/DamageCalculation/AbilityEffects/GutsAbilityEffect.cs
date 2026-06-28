using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Modifiers;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;

/// <summary>
/// 根性: 状態異常のとき攻撃を 1.5 倍にし、やけどの攻撃半減を無視する。
/// </summary>
public sealed class GutsAbilityEffect : IAbilityStatusAilmentEffect, IAbilityAttackStatEffect
{
    public bool IgnoresPrimaryStatusAilmentPenalty(
        BattlePokemon subject,
        PrimaryStatusAilment primaryAilment,
        DamageSpec spec,
        DamageContext context)
    {
        return primaryAilment == PrimaryStatusAilment.Burn
            && subject.StatusAilment.HasPrimaryAilment
            && spec.AttackSource == StatSelector.Attack;
    }

    public AttackStatModifier GetAttackStatModifier(
        BattlePokemon subject,
        DamageSpec spec,
        DamageContext context)
    {
        if (!subject.StatusAilment.HasPrimaryAilment)
        {
            return AttackStatModifier.None;
        }

        if (spec.AttackSource != StatSelector.Attack)
        {
            return AttackStatModifier.None;
        }

        return AttackStatModifier.Boost;
    }
}