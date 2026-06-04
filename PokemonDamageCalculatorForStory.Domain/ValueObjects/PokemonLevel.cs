using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンのレベルを表します。
/// </summary>
public readonly record struct PokemonLevel
{
    /// <summary>
    /// 許容される最小値です。
    /// </summary>
    public const int MinValue = 1;

    /// <summary>
    /// 許容される最大値です。
    /// </summary>
    public const int MaxValue = 100;

    /// <summary>
    /// レベル値です。
    /// </summary>
    public int Value { get; }

    private PokemonLevel(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="PokemonLevel"/> を生成します。
    /// </summary>
    /// <param name="value">レベル値。</param>
    /// <returns>生成されたレベル。</returns>
    public static PokemonLevel Create(int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ValidationException($"Level must be between {MinValue} and {MaxValue}.");
        }

        return new PokemonLevel(value);
    }
}
