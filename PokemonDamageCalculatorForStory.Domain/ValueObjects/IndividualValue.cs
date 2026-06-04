using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの個体値を表します。
/// </summary>
public readonly record struct IndividualValue
{
    /// <summary>
    /// 許容される最小値です。
    /// </summary>
    public const int MinValue = 0;

    /// <summary>
    /// 許容される最大値です。
    /// </summary>
    public const int MaxValue = 31;

    /// <summary>
    /// 個体値です。
    /// </summary>
    public int Value { get; }

    private IndividualValue(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="IndividualValue"/> を生成します。
    /// </summary>
    /// <param name="value">個体値。</param>
    /// <param name="group">所属するステータスグループ。</param>
    /// <param name="field">対象ステータス。</param>
    /// <returns>生成された個体値。</returns>
    internal static IndividualValue Create(int value, SnapshotValueGroup group, SnapshotStatField field)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ValidationException($"{SnapshotFieldName.Format(group, field)} must be between {MinValue} and {MaxValue}.");
        }

        return new IndividualValue(value);
    }
}
