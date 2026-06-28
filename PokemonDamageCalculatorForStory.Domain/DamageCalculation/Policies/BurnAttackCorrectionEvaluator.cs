using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;

/// <summary>
/// やけどによる攻撃補正値の要否を判定する。
/// </summary>
internal static class BurnAttackCorrectionEvaluator
{
    /// <summary>
    /// ダメージ計算文脈からやけど補正値を算出する。
    /// </summary>
    /// <param name="spec">ダメージ計算仕様。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <returns>やけど補正値。</returns>
    public static BurnAttackCorrection Evaluate(DamageSpec spec, DamageContext context)
    {
        // やけど補正は物理技でのみ適用される。
        if (spec.FixedDamage > 0 || context.Move.Category != MoveCategory.Physical)
        {
            return BurnAttackCorrection.None;
        }

        // 攻撃側のポケモンがやけど状態でなければ補正を適用しない。
        var attackPokemon = context.GetAttackStatPokemon(spec.AttackStatOwner);
        if (attackPokemon.StatusAilment.PrimaryAilment != PrimaryStatusAilment.Burn)
        {
            return BurnAttackCorrection.None;
        }


        return BurnAttackCorrection.Halved;
    }
}