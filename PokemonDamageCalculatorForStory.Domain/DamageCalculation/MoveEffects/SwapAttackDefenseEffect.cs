using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;

/// <summary>
/// 攻撃と防御の参照元を入れ替える効果を表す。
/// </summary>
public class SwapAttackDefenseEffect : IMoveDamageEffect
{
    /// <summary>
    /// 攻撃防御の参照元入れ替えを適用する。
    /// </summary>
    /// <param name="spec">現在のダメージ計算仕様。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <returns>効果適用後のダメージ計算仕様。</returns>
    /// <inheritdoc />
    public DamageSpec Apply(DamageSpec spec, DamageContext context)
    {
        return new DamageSpec(
            spec.DefenseSource,
            spec.AttackStatOwner,
            spec.AttackSource,
            spec.PowerOverride,
            spec.FixedDamage);
    }
}