namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

internal static class SnapshotFieldName
{
    private static readonly IReadOnlyDictionary<string, SnapshotStatField> FieldMap = new Dictionary<string, SnapshotStatField>(StringComparer.OrdinalIgnoreCase)
    {
        ["Hp"] = SnapshotStatField.Hp,
        ["Attack"] = SnapshotStatField.Attack,
        ["Defense"] = SnapshotStatField.Defense,
        ["SpecialAttack"] = SnapshotStatField.SpecialAttack,
        ["SpecialDefense"] = SnapshotStatField.SpecialDefense,
        ["Speed"] = SnapshotStatField.Speed
    };

    internal static IReadOnlyList<SnapshotStatField> AllFields { get; } =
    [
        SnapshotStatField.Hp,
        SnapshotStatField.Attack,
        SnapshotStatField.Defense,
        SnapshotStatField.SpecialAttack,
        SnapshotStatField.SpecialDefense,
        SnapshotStatField.Speed
    ];

    internal static string Format(SnapshotValueGroup group, SnapshotStatField field)
        => $"{GetGroupName(group)}.{GetFieldName(field)}";

    internal static string GetGroupName(SnapshotValueGroup group)
        => group switch
        {
            SnapshotValueGroup.Stats => "Stats",
            SnapshotValueGroup.EVs => "EVs",
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };

    internal static string GetFieldName(SnapshotStatField field)
        => field switch
        {
            SnapshotStatField.Hp => "Hp",
            SnapshotStatField.Attack => "Attack",
            SnapshotStatField.Defense => "Defense",
            SnapshotStatField.SpecialAttack => "SpecialAttack",
            SnapshotStatField.SpecialDefense => "SpecialDefense",
            SnapshotStatField.Speed => "Speed",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };

    internal static bool TryParse(string propertyName, out SnapshotStatField field)
        => FieldMap.TryGetValue(propertyName, out field);
}
