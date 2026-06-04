using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの種族値を表します。
/// </summary>
public readonly record struct BaseStat
{
    /// <summary>
    /// 許容される最小値です。
    /// </summary>
    public const int MinValue = 1;

    /// <summary>
    /// 許容される最大値です。
    /// </summary>
    public const int MaxValue = 255;

    /// <summary>
    /// 種族値です。
    /// </summary>
    public int Value { get; }

    private BaseStat(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="BaseStat"/> を生成します。
    /// </summary>
    /// <param name="value">種族値。</param>
    /// <param name="group">所属するステータスグループ。</param>
    /// <param name="field">対象ステータス。</param>
    /// <returns>生成された種族値。</returns>
    internal static BaseStat Create(int value, SnapshotValueGroup group, SnapshotStatField field)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ValidationException($"{SnapshotFieldName.Format(group, field)} must be between {MinValue} and {MaxValue}.");
        }

        return new BaseStat(value);
    }
}
