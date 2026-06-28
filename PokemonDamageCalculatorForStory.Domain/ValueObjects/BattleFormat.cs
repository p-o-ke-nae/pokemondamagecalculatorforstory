namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 戦闘の対戦形式を表す。
/// </summary>
public enum BattleFormat
{
    /// <summary>シングルバトル。</summary>
    Single,

    /// <summary>ダブルバトル。</summary>
    Double,

    /// <summary>トリプルバトル。</summary>
    Triple
}