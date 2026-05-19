using System.Text.Json;
using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

internal static class SnapshotValuesJsonParser
{
    public static ParsedSnapshotValues Parse(string json, SnapshotValueGroup group)
    {
        var groupName = SnapshotFieldName.GetGroupName(group);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ValidationException($"{groupName} is required.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationException($"{groupName} must be a JSON object.");
            }

            var values = new Dictionary<SnapshotStatField, int>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!SnapshotFieldName.TryParse(property.Name, out var field))
                {
                    throw new ValidationException($"{groupName} contains unsupported field '{property.Name}'.");
                }

                if (!values.TryAdd(field, ReadInt32(property.Value, SnapshotFieldName.Format(group, field))))
                {
                    throw new ValidationException($"{groupName} contains duplicate field '{property.Name}'.");
                }
            }

            var missingFields = SnapshotFieldName.AllFields
                .Where(field => !values.ContainsKey(field))
                .Select(SnapshotFieldName.GetFieldName)
                .ToArray();

            if (missingFields.Length > 0)
            {
                throw new ValidationException($"{groupName} is missing required fields: {string.Join(", ", missingFields)}.");
            }

            return new ParsedSnapshotValues(
                values[SnapshotStatField.Hp],
                values[SnapshotStatField.Attack],
                values[SnapshotStatField.Defense],
                values[SnapshotStatField.SpecialAttack],
                values[SnapshotStatField.SpecialDefense],
                values[SnapshotStatField.Speed]);
        }
        catch (JsonException)
        {
            throw new ValidationException($"{groupName} must be valid JSON.");
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
}
