namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;

/// <summary>
/// 世代別のダメージ計算ポリシーを表す。
/// </summary>
public interface IPokemonDamagePolicy
{
    /// <summary>
    /// ダメージ計算仕様と戦闘文脈からダメージ結果を算出する。
    /// </summary>
    /// <param name="spec">ダメージ計算仕様。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <returns>ダメージ結果。</returns>
    DamageResult Calculate(DamageSpec spec, DamageContext context);
}