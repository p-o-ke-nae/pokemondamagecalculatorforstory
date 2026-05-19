namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの各個体値を表します。
/// </summary>
/// <param name="Hp">HP 個体値。</param>
/// <param name="Attack">こうげき個体値。</param>
/// <param name="Defense">ぼうぎょ個体値。</param>
/// <param name="SpecialAttack">とくこう個体値。</param>
/// <param name="SpecialDefense">とくぼう個体値。</param>
/// <param name="Speed">すばやさ個体値。</param>
public readonly record struct IndividualValues(
    IndividualValue Hp,
    IndividualValue Attack,
    IndividualValue Defense,
    IndividualValue SpecialAttack,
    IndividualValue SpecialDefense,
    IndividualValue Speed)
{
    /// <summary>
    /// 値から <see cref="IndividualValues"/> を生成します。
    /// </summary>
    public static IndividualValues Create(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
        => new(
            IndividualValue.Create(hp, SnapshotValueGroup.IVs, SnapshotStatField.Hp),
            IndividualValue.Create(attack, SnapshotValueGroup.IVs, SnapshotStatField.Attack),
            IndividualValue.Create(defense, SnapshotValueGroup.IVs, SnapshotStatField.Defense),
            IndividualValue.Create(specialAttack, SnapshotValueGroup.IVs, SnapshotStatField.SpecialAttack),
            IndividualValue.Create(specialDefense, SnapshotValueGroup.IVs, SnapshotStatField.SpecialDefense),
            IndividualValue.Create(speed, SnapshotValueGroup.IVs, SnapshotStatField.Speed));

    /// <summary>
    /// JSON 文字列から <see cref="IndividualValues"/> を生成します。
    /// </summary>
    /// <param name="json">個体値 JSON。</param>
    /// <returns>生成された個体値群。</returns>
    public static IndividualValues FromJson(string json)
    {
        var values = SnapshotValuesJsonParser.Parse(json, SnapshotValueGroup.IVs);
        return Create(values.Hp, values.Attack, values.Defense, values.SpecialAttack, values.SpecialDefense, values.Speed);
    }

    /// <summary>
    /// 個体値群を正規化済み JSON に変換します。
    /// </summary>
    /// <returns>正規化済み JSON。</returns>
    public string ToJson()
        => SnapshotValuesJsonParser.Serialize(Hp.Value, Attack.Value, Defense.Value, SpecialAttack.Value, SpecialDefense.Value, Speed.Value);
}
