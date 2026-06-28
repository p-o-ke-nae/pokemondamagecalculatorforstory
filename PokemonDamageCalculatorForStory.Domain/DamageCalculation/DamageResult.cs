namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation;

/// <summary>
/// ダメージ計算結果を表す。
/// </summary>
public class DamageResult
{
    private readonly int[] _damageRolls;

    /// <summary>
    /// ダメージ結果を初期化する。
    /// </summary>
    /// <param name="damageRolls">ダメージ乱数列。</param>
    public DamageResult(int[] damageRolls)
    {
        _damageRolls = damageRolls.ToArray();
    }

    /// <summary>ダメージ乱数列。</summary>
    public int[] DamageRolls => _damageRolls.ToArray();
}