namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.Modifiers;

/// <summary>
/// やけどによる攻撃補正値を表す。
/// </summary>
internal readonly struct BurnAttackModifier
{
    /// <summary>
    /// 補正値を初期化する。
    /// </summary>
    /// <param name="numerator">分子。</param>
    /// <param name="denominator">分母。</param>
    public BurnAttackModifier(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>補正なし。</summary>
    public static BurnAttackModifier None { get; } = new(1, 1);

    /// <summary>やけどによる半減補正。</summary>
    public static BurnAttackModifier Halved { get; } = new(1, 2);

    /// <summary>分子。</summary>
    public int Numerator { get; }

    /// <summary>分母。</summary>
    public int Denominator { get; }

    /// <summary>
    /// 補正値を整数値へ適用する。
    /// </summary>
    /// <param name="value">補正対象値。</param>
    /// <returns>補正適用後の値。</returns>
    public int ApplyTo(int value)
    {
        return value * Numerator / Denominator;
    }
}