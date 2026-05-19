using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 努力値を表します。
/// </summary>
public readonly record struct EffortValue
{
    /// <summary>
    /// 許容される最小値です。
    /// </summary>
    public const int MinValue = 0;

    /// <summary>
    /// 許容される最大値です。
    /// </summary>
    public const int MaxValue = 252;

    /// <summary>
    /// 努力値です。
    /// </summary>
    public int Value { get; }

    private EffortValue(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="EffortValue"/> を生成します。
    /// </summary>
    /// <param name="value">努力値。</param>
    /// <param name="group">所属するステータスグループ。</param>
    /// <param name="field">対象ステータス。</param>
    /// <returns>生成された努力値。</returns>
    internal static EffortValue Create(int value, SnapshotValueGroup group, SnapshotStatField field)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ValidationException($"{SnapshotFieldName.Format(group, field)} must be between {MinValue} and {MaxValue}.");
        }

        return new EffortValue(value);
    }
}
