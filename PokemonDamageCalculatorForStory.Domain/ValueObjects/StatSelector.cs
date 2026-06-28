namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ダメージ計算で参照する能力種別を表す。
/// </summary>
public enum StatSelector
{
    /// <summary>HP。</summary>
    Hp,
    
    /// <summary>こうげき。</summary>
    Attack,

    /// <summary>ぼうぎょ。</summary>
    Defense,

    /// <summary>とくこう。</summary>
    SpecialAttack,

    /// <summary>とくぼう。</summary>
    SpecialDefense,

    /// <summary>すばやさ。</summary>
    Speed
}