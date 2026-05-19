namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの各実数値を表します。
/// </summary>
/// <param name="Hp">HP 実数値。</param>
/// <param name="Attack">こうげき実数値。</param>
/// <param name="Defense">ぼうぎょ実数値。</param>
/// <param name="SpecialAttack">とくこう実数値。</param>
/// <param name="SpecialDefense">とくぼう実数値。</param>
/// <param name="Speed">すばやさ実数値。</param>
public readonly record struct PokemonStats(
    PokemonStat Hp,
    PokemonStat Attack,
    PokemonStat Defense,
    PokemonStat SpecialAttack,
    PokemonStat SpecialDefense,
    PokemonStat Speed)
{
    /// <summary>
    /// 値から <see cref="PokemonStats"/> を生成します。
    /// </summary>
    public static PokemonStats Create(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
        => new(
            PokemonStat.Create(hp, SnapshotValueGroup.Stats, SnapshotStatField.Hp),
            PokemonStat.Create(attack, SnapshotValueGroup.Stats, SnapshotStatField.Attack),
            PokemonStat.Create(defense, SnapshotValueGroup.Stats, SnapshotStatField.Defense),
            PokemonStat.Create(specialAttack, SnapshotValueGroup.Stats, SnapshotStatField.SpecialAttack),
            PokemonStat.Create(specialDefense, SnapshotValueGroup.Stats, SnapshotStatField.SpecialDefense),
            PokemonStat.Create(speed, SnapshotValueGroup.Stats, SnapshotStatField.Speed));

    /// <summary>
    /// JSON 文字列から <see cref="PokemonStats"/> を生成します。
    /// </summary>
    /// <param name="json">実数値 JSON。</param>
    /// <returns>生成された実数値群。</returns>
    public static PokemonStats FromJson(string json)
    {
        var values = SnapshotValuesJsonParser.Parse(json, SnapshotValueGroup.Stats);
        return Create(values.Hp, values.Attack, values.Defense, values.SpecialAttack, values.SpecialDefense, values.Speed);
    }

    /// <summary>
    /// 実数値群を正規化済み JSON に変換します。
    /// </summary>
    /// <returns>正規化済み JSON。</returns>
    public string ToJson()
        => SnapshotValuesJsonParser.Serialize(Hp.Value, Attack.Value, Defense.Value, SpecialAttack.Value, SpecialDefense.Value, Speed.Value);
}
