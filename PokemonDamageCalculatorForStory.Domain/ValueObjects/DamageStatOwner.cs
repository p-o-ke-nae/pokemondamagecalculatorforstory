namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ダメージ計算で攻撃値を参照するポケモンを表す。
/// </summary>
public enum DamageStatOwner
{
    /// <summary>攻撃側ポケモン。</summary>
    Attacker,

    /// <summary>防御側ポケモン。</summary>
    Defender,
}