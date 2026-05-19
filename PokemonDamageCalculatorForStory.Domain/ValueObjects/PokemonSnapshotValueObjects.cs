using System.Text.Json;
using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

public readonly record struct PokemonStat
{
    public const int MinValue = 1;

    public int Value { get; }

    private PokemonStat(int value)
    {
        Value = value;
    }

    public static PokemonStat Create(int value, string fieldName)
    {
        if (value < MinValue)
        {
            throw new ValidationException($"{fieldName} must be greater than zero.");
        }

        return new PokemonStat(value);
    }
}

public readonly record struct EffortValue
{
    public const int MinValue = 0;
    public const int MaxValue = 252;

    public int Value { get; }

    private EffortValue(int value)
    {
        Value = value;
    }

    public static EffortValue Create(int value, string fieldName)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ValidationException($"{fieldName} must be between {MinValue} and {MaxValue}.");
        }

        return new EffortValue(value);
    }
}

public readonly record struct PokemonStats(
    PokemonStat Hp,
    PokemonStat Attack,
    PokemonStat Defense,
    PokemonStat SpecialAttack,
    PokemonStat SpecialDefense,
    PokemonStat Speed)
{
    public static PokemonStats Create(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
        => new(
            PokemonStat.Create(hp, "Stats.Hp"),
            PokemonStat.Create(attack, "Stats.Attack"),
            PokemonStat.Create(defense, "Stats.Defense"),
            PokemonStat.Create(specialAttack, "Stats.SpecialAttack"),
            PokemonStat.Create(specialDefense, "Stats.SpecialDefense"),
            PokemonStat.Create(speed, "Stats.Speed"));

    public static PokemonStats FromJson(string json)
    {
        var values = SnapshotValuesJsonParser.Parse(json, "Stats");
        return Create(values.Hp, values.Attack, values.Defense, values.SpecialAttack, values.SpecialDefense, values.Speed);
    }

    public string ToJson()
        => SnapshotValuesJsonParser.Serialize(Hp.Value, Attack.Value, Defense.Value, SpecialAttack.Value, SpecialDefense.Value, Speed.Value);
}

public readonly record struct EffortValues(
    EffortValue Hp,
    EffortValue Attack,
    EffortValue Defense,
    EffortValue SpecialAttack,
    EffortValue SpecialDefense,
    EffortValue Speed)
{
    public const int MaxTotalValue = 510;

    public int Total => Hp.Value + Attack.Value + Defense.Value + SpecialAttack.Value + SpecialDefense.Value + Speed.Value;

    public static EffortValues Create(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
    {
        var values = new EffortValues(
            EffortValue.Create(hp, "EVs.Hp"),
            EffortValue.Create(attack, "EVs.Attack"),
            EffortValue.Create(defense, "EVs.Defense"),
            EffortValue.Create(specialAttack, "EVs.SpecialAttack"),
            EffortValue.Create(specialDefense, "EVs.SpecialDefense"),
            EffortValue.Create(speed, "EVs.Speed"));

        if (values.Total > MaxTotalValue)
        {
            throw new ValidationException($"EV total must be less than or equal to {MaxTotalValue}.");
        }

        return values;
    }

    public static EffortValues FromJson(string json)
    {
        var values = SnapshotValuesJsonParser.Parse(json, "EVs");
        return Create(values.Hp, values.Attack, values.Defense, values.SpecialAttack, values.SpecialDefense, values.Speed);
    }

    public string ToJson()
        => SnapshotValuesJsonParser.Serialize(Hp.Value, Attack.Value, Defense.Value, SpecialAttack.Value, SpecialDefense.Value, Speed.Value);
}

internal static class SnapshotValuesJsonParser
{
    private static readonly string[] RequiredPropertyNames =
    [
        "Hp",
        "Attack",
        "Defense",
        "SpecialAttack",
        "SpecialDefense",
        "Speed"
    ];

    public static ParsedSnapshotValues Parse(string json, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ValidationException($"{fieldName} is required.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationException($"{fieldName} must be a JSON object.");
            }

            var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!TryNormalizePropertyName(property.Name, out var normalizedPropertyName))
                {
                    throw new ValidationException($"{fieldName} contains unsupported field '{property.Name}'.");
                }

                if (!values.TryAdd(normalizedPropertyName, ReadInt32(property.Value, $"{fieldName}.{normalizedPropertyName}")))
                {
                    throw new ValidationException($"{fieldName} contains duplicate field '{property.Name}'.");
                }
            }

            var missingPropertyNames = RequiredPropertyNames.Where(propertyName => !values.ContainsKey(propertyName)).ToArray();
            if (missingPropertyNames.Length > 0)
            {
                throw new ValidationException($"{fieldName} is missing required fields: {string.Join(", ", missingPropertyNames)}.");
            }

            return new ParsedSnapshotValues(
                values["Hp"],
                values["Attack"],
                values["Defense"],
                values["SpecialAttack"],
                values["SpecialDefense"],
                values["Speed"]);
        }
        catch (JsonException)
        {
            throw new ValidationException($"{fieldName} must be valid JSON.");
        }
    }

    public static string Serialize(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
        => JsonSerializer.Serialize(new SnapshotValuesDocument(hp, attack, defense, specialAttack, specialDefense, speed));

    private static int ReadInt32(JsonElement element, string fieldName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new ValidationException($"{fieldName} must be an integer.");
        }

        return value;
    }

    private static bool TryNormalizePropertyName(string propertyName, out string normalizedPropertyName)
    {
        if (string.Equals(propertyName, "Hp", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPropertyName = "Hp";
            return true;
        }

        if (string.Equals(propertyName, "Attack", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPropertyName = "Attack";
            return true;
        }

        if (string.Equals(propertyName, "Defense", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPropertyName = "Defense";
            return true;
        }

        if (string.Equals(propertyName, "SpecialAttack", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPropertyName = "SpecialAttack";
            return true;
        }

        if (string.Equals(propertyName, "SpecialDefense", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPropertyName = "SpecialDefense";
            return true;
        }

        if (string.Equals(propertyName, "Speed", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPropertyName = "Speed";
            return true;
        }

        normalizedPropertyName = string.Empty;
        return false;
    }

    internal readonly record struct ParsedSnapshotValues(
        int Hp,
        int Attack,
        int Defense,
        int SpecialAttack,
        int SpecialDefense,
        int Speed);

    private sealed record SnapshotValuesDocument(
        int Hp,
        int Attack,
        int Defense,
        int SpecialAttack,
        int SpecialDefense,
        int Speed);
}
