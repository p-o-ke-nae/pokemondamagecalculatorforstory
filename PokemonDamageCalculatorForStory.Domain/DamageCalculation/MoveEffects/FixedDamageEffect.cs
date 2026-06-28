namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;

/// <summary>
/// 固定ダメージ系の技効果を表す。
/// </summary>
public class FixedDamageEffect : IMoveDamageEffect
{
    /// <summary>
    /// 固定ダメージ効果を適用する。
    /// </summary>
    /// <param name="spec">現在のダメージ計算仕様。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <returns>効果適用後のダメージ計算仕様。</returns>
    /// <inheritdoc />
    public DamageSpec Apply(DamageSpec spec, DamageContext context)
    {
        return spec;
    }
}