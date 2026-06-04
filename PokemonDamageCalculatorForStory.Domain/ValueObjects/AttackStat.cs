using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 攻撃実数値を表します。
/// </summary>
public readonly record struct AttackStat
{
    /// <summary>
    /// 攻撃実数値です。
    /// </summary>
    public int Value { get; }

    private AttackStat(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="AttackStat"/> を生成します。
    /// </summary>
    /// <param name="value">攻撃実数値。</param>
    /// <returns>生成された攻撃実数値。</returns>
    public static AttackStat Create(int value)
    {
        if (value <= 0)
        {
            throw new ValidationException("AttackStat must be greater than zero.");
        }

        return new AttackStat(value);
    }
}
