using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 技の威力を表します。
/// </summary>
public readonly record struct MovePower
{
    /// <summary>
    /// 技の威力です。
    /// </summary>
    public int Value { get; }

    private MovePower(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="MovePower"/> を生成します。
    /// </summary>
    /// <param name="value">技の威力。</param>
    /// <returns>生成された技の威力。</returns>
    public static MovePower Create(int value)
    {
        if (value <= 0)
        {
            throw new ValidationException("MovePower must be greater than zero.");
        }

        return new MovePower(value);
    }
}
