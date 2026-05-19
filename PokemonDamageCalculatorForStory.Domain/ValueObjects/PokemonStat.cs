using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの実数値を表します。
/// </summary>
public readonly record struct PokemonStat
{
    /// <summary>
    /// 許容される最小値です。
    /// </summary>
    public const int MinValue = 1;

    /// <summary>
    /// 実数値です。
    /// </summary>
    public int Value { get; }

    private PokemonStat(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 値から <see cref="PokemonStat"/> を生成します。
    /// </summary>
    /// <param name="value">実数値。</param>
    /// <param name="group">所属するステータスグループ。</param>
    /// <param name="field">対象ステータス。</param>
    /// <returns>生成された実数値。</returns>
    internal static PokemonStat Create(int value, SnapshotValueGroup group, SnapshotStatField field)
    {
        if (value < MinValue)
        {
            throw new ValidationException($"{SnapshotFieldName.Format(group, field)} must be greater than zero.");
        }

        return new PokemonStat(value);
    }
}
