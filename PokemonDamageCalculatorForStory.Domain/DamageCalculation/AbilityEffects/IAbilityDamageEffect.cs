namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;

/// <summary>
/// 特性によるダメージ効果を表す。
/// </summary>
public interface IAbilityDamageEffect
{
    /// <summary>
    /// ダメージ計算仕様へ特性の効果を適用する。
    /// </summary>
    /// <param name="spec">現在のダメージ計算仕様。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <returns>効果適用後のダメージ計算仕様。</returns>
    DamageSpec Apply(DamageSpec spec, DamageContext context);
}