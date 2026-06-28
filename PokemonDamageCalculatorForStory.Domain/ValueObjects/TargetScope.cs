namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 技の対象範囲を表す。
/// </summary>
public enum TargetScope
{
    /// <summary>自分自身。</summary>
    Self,

    /// <summary>単体の相手。</summary>
    SingleOpponent,

    /// <summary>複数の相手。</summary>
    AllOpponents,

    /// <summary>場全体。</summary>
    AllBattlers
}