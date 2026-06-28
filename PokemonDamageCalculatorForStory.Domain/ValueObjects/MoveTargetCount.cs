namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 技の対象数を表す。
/// </summary>
public readonly struct MoveTargetCount
{
    /// <summary>
    /// 技の対象数を初期化する。
    /// </summary>
    /// <param name="value">対象数。</param>
    public MoveTargetCount(int value)
    {
        Value = value;
    }

    /// <summary>対象数。</summary>
    public int Value { get; }
}