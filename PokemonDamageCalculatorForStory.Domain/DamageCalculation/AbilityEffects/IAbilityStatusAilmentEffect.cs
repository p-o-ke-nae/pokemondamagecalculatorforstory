using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;

/// <summary>
/// 特性による状態異常効果を表す。
/// </summary>
public interface IAbilityStatusAilmentEffect
{
    /// <summary>
    /// 指定した主要状態異常による不利益補正を無視するかを判定する。
    /// </summary>
    bool IgnoresPrimaryStatusAilmentPenalty(
        BattlePokemon subject,
        PrimaryStatusAilment primaryAilment,
        DamageSpec spec,
        DamageContext context);
}