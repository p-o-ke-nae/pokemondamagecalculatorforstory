using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの各努力値を表します。
/// </summary>
/// <param name="Hp">HP 努力値。</param>
/// <param name="Attack">こうげき努力値。</param>
/// <param name="Defense">ぼうぎょ努力値。</param>
/// <param name="SpecialAttack">とくこう努力値。</param>
/// <param name="SpecialDefense">とくぼう努力値。</param>
/// <param name="Speed">すばやさ努力値。</param>
public readonly record struct EffortValues(
    EffortValue Hp,
    EffortValue Attack,
    EffortValue Defense,
    EffortValue SpecialAttack,
    EffortValue SpecialDefense,
    EffortValue Speed)
{
    /// <summary>
    /// 努力値合計の最大値です。
    /// </summary>
    public const int MaxTotalValue = 510;

    /// <summary>
    /// 努力値の合計値です。
    /// </summary>
    public int Total => Hp.Value + Attack.Value + Defense.Value + SpecialAttack.Value + SpecialDefense.Value + Speed.Value;

    /// <summary>
    /// 値から <see cref="EffortValues"/> を生成します。
    /// </summary>
    public static EffortValues Create(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
    {
        var values = new EffortValues(
            EffortValue.Create(hp, SnapshotValueGroup.EVs, SnapshotStatField.Hp),
            EffortValue.Create(attack, SnapshotValueGroup.EVs, SnapshotStatField.Attack),
            EffortValue.Create(defense, SnapshotValueGroup.EVs, SnapshotStatField.Defense),
            EffortValue.Create(specialAttack, SnapshotValueGroup.EVs, SnapshotStatField.SpecialAttack),
            EffortValue.Create(specialDefense, SnapshotValueGroup.EVs, SnapshotStatField.SpecialDefense),
            EffortValue.Create(speed, SnapshotValueGroup.EVs, SnapshotStatField.Speed));

        if (values.Total > MaxTotalValue)
        {
            throw new ValidationException($"EV total must be less than or equal to {MaxTotalValue}.");
        }

        return values;
    }

    /// <summary>
    /// JSON 文字列から <see cref="EffortValues"/> を生成します。
    /// </summary>
    /// <param name="json">努力値 JSON。</param>
    /// <returns>生成された努力値群。</returns>
    public static EffortValues FromJson(string json)
    {
        var values = SnapshotValuesJsonParser.Parse(json, SnapshotValueGroup.EVs);
        return Create(values.Hp, values.Attack, values.Defense, values.SpecialAttack, values.SpecialDefense, values.Speed);
    }

    /// <summary>
    /// 努力値群を正規化済み JSON に変換します。
    /// </summary>
    /// <returns>正規化済み JSON。</returns>
    public string ToJson()
        => SnapshotValuesJsonParser.Serialize(Hp.Value, Attack.Value, Defense.Value, SpecialAttack.Value, SpecialDefense.Value, Speed.Value);
}
