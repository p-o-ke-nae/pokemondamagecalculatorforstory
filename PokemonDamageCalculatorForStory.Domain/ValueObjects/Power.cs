namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 技威力を表す。
/// </summary>
public readonly struct Power
{
    /// <summary>
    /// 技威力を初期化する。
    /// </summary>
    /// <param name="value">威力値。</param>
    public Power(int value)
    {
        Value = value;
    }

    /// <summary>威力値。</summary>
    public int Value { get; }
}