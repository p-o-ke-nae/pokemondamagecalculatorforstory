namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンのレベルを表す。
/// </summary>
public readonly struct Level
{
    /// <summary>
    /// レベルを初期化する。
    /// </summary>
    /// <param name="value">レベル値。</param>
    public Level(int value)
    {
        Value = value;
    }

    /// <summary>レベル値。</summary>
    public int Value { get; }
}