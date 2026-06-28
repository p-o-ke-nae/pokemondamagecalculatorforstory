namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 天候状態を表す。
/// </summary>
public enum Weather
{
    /// <summary>天候なし。</summary>
    None,

    /// <summary>にほんばれ。</summary>
    Sun,

    /// <summary>あめ。</summary>
    Rain,

    /// <summary>すなあらし。</summary>
    Sandstorm,

    /// <summary>ゆき。</summary>
    Snow
}