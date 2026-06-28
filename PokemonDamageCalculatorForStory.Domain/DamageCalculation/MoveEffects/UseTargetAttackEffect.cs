using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;

/// <summary>
/// 対象側の攻撃値を参照する技効果を表す。
/// </summary>
public class UseTargetAttackEffect : IMoveDamageEffect
{
    /// <summary>
    /// 対象攻撃参照効果を適用する。
    /// </summary>
    /// <param name="spec">現在のダメージ計算仕様。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <returns>効果適用後のダメージ計算仕様。</returns>
    /// <inheritdoc />
    public DamageSpec Apply(DamageSpec spec, DamageContext context)
    {
        return new DamageSpec(
            spec.AttackSource,
            DamageStatOwner.Defender,
            spec.DefenseSource,
            spec.PowerOverride,
            spec.FixedDamage);
    }
}