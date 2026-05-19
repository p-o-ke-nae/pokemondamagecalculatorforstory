namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモン種族の各種族値を表します。
/// </summary>
/// <param name="Hp">HP 種族値。</param>
/// <param name="Attack">こうげき種族値。</param>
/// <param name="Defense">ぼうぎょ種族値。</param>
/// <param name="SpecialAttack">とくこう種族値。</param>
/// <param name="SpecialDefense">とくぼう種族値。</param>
/// <param name="Speed">すばやさ種族値。</param>
public readonly record struct SpeciesBaseStats(
    BaseStat Hp,
    BaseStat Attack,
    BaseStat Defense,
    BaseStat SpecialAttack,
    BaseStat SpecialDefense,
    BaseStat Speed)
{
    /// <summary>
    /// 値から <see cref="SpeciesBaseStats"/> を生成します。
    /// </summary>
    public static SpeciesBaseStats Create(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
        => new(
            BaseStat.Create(hp, SnapshotValueGroup.BaseStats, SnapshotStatField.Hp),
            BaseStat.Create(attack, SnapshotValueGroup.BaseStats, SnapshotStatField.Attack),
            BaseStat.Create(defense, SnapshotValueGroup.BaseStats, SnapshotStatField.Defense),
            BaseStat.Create(specialAttack, SnapshotValueGroup.BaseStats, SnapshotStatField.SpecialAttack),
            BaseStat.Create(specialDefense, SnapshotValueGroup.BaseStats, SnapshotStatField.SpecialDefense),
            BaseStat.Create(speed, SnapshotValueGroup.BaseStats, SnapshotStatField.Speed));

    /// <summary>
    /// JSON 文字列から <see cref="SpeciesBaseStats"/> を生成します。
    /// </summary>
    /// <param name="json">種族値 JSON。</param>
    /// <returns>生成された種族値群。</returns>
    public static SpeciesBaseStats FromJson(string json)
    {
        var values = SnapshotValuesJsonParser.Parse(json, SnapshotValueGroup.BaseStats);
        return Create(values.Hp, values.Attack, values.Defense, values.SpecialAttack, values.SpecialDefense, values.Speed);
    }

    /// <summary>
    /// 種族値群を正規化済み JSON に変換します。
    /// </summary>
    /// <returns>正規化済み JSON。</returns>
    public string ToJson()
        => SnapshotValuesJsonParser.Serialize(Hp.Value, Attack.Value, Defense.Value, SpecialAttack.Value, SpecialDefense.Value, Speed.Value);
}
