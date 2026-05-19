using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// タイプ相性倍率を表します。
/// </summary>
public readonly record struct TypeEffectivenessMultiplier
{
    /// <summary>
    /// タイプ相性倍率です。
    /// </summary>
    public float Value { get; }

    private TypeEffectivenessMultiplier(float value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="TypeEffectivenessMultiplier"/> を生成します。
    /// </summary>
    /// <param name="value">タイプ相性倍率。</param>
    /// <returns>生成されたタイプ相性倍率。</returns>
    public static TypeEffectivenessMultiplier Create(float value)
    {
        if (value <= 0)
        {
            throw new ValidationException("TypeEffectiveness must be greater than zero.");
        }

        return new TypeEffectivenessMultiplier(value);
    }
}
