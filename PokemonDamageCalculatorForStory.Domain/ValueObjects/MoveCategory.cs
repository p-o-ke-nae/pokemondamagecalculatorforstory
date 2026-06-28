namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 技の分類を表す。
/// </summary>
public enum MoveCategory
{
    /// <summary>物理技。</summary>
    Physical,

    /// <summary>特殊技。</summary>
    Special,

    /// <summary>変化技。</summary>
    Status
}