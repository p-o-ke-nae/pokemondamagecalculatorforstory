namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;

/// <summary>
/// 全体技の威力補正を表す。
/// </summary>
public class SpreadMovePowerEffect : IMoveDamageEffect
{
    /// <summary>
    /// 全体技補正を適用する。
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