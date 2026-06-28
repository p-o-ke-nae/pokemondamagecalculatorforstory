namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 場の地形効果を表す。
/// </summary>
public enum Terrain
{
    /// <summary>地形効果なし。</summary>
    None,

    /// <summary>エレキフィールド。</summary>
    Electric,

    /// <summary>グラスフィールド。</summary>
    Grassy,

    /// <summary>ミストフィールド。</summary>
    Misty,

    /// <summary>サイコフィールド。</summary>
    Psychic
}