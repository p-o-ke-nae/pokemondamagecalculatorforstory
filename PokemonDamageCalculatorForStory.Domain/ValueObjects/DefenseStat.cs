using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 防御実数値を表します。
/// </summary>
public readonly record struct DefenseStat
{
    /// <summary>
    /// 防御実数値です。
    /// </summary>
    public int Value { get; }

    private DefenseStat(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="DefenseStat"/> を生成します。
    /// </summary>
    /// <param name="value">防御実数値。</param>
    /// <returns>生成された防御実数値。</returns>
    public static DefenseStat Create(int value)
    {
        if (value <= 0)
        {
            throw new ValidationException("DefenseStat must be greater than zero.");
        }

        return new DefenseStat(value);
    }
}
