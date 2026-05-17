using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Application.Services;

/// <summary>タイプ相性表を提供します。</summary>
public static class PokemonTypeChart
{
    private static readonly IReadOnlyDictionary<(PokemonType Attack, PokemonType Defense), decimal> Multipliers =
        new Dictionary<(PokemonType Attack, PokemonType Defense), decimal>
        {
            [(PokemonType.Normal, PokemonType.Rock)] = 0.5m,
            [(PokemonType.Normal, PokemonType.Ghost)] = 0m,
            [(PokemonType.Normal, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Fire, PokemonType.Fire)] = 0.5m,
            [(PokemonType.Fire, PokemonType.Water)] = 0.5m,
            [(PokemonType.Fire, PokemonType.Grass)] = 2m,
            [(PokemonType.Fire, PokemonType.Ice)] = 2m,
            [(PokemonType.Fire, PokemonType.Bug)] = 2m,
            [(PokemonType.Fire, PokemonType.Rock)] = 0.5m,
            [(PokemonType.Fire, PokemonType.Dragon)] = 0.5m,
            [(PokemonType.Fire, PokemonType.Steel)] = 2m,
            [(PokemonType.Water, PokemonType.Fire)] = 2m,
            [(PokemonType.Water, PokemonType.Water)] = 0.5m,
            [(PokemonType.Water, PokemonType.Grass)] = 0.5m,
            [(PokemonType.Water, PokemonType.Ground)] = 2m,
            [(PokemonType.Water, PokemonType.Rock)] = 2m,
            [(PokemonType.Water, PokemonType.Dragon)] = 0.5m,
            [(PokemonType.Electric, PokemonType.Water)] = 2m,
            [(PokemonType.Electric, PokemonType.Electric)] = 0.5m,
            [(PokemonType.Electric, PokemonType.Grass)] = 0.5m,
            [(PokemonType.Electric, PokemonType.Ground)] = 0m,
            [(PokemonType.Electric, PokemonType.Flying)] = 2m,
            [(PokemonType.Electric, PokemonType.Dragon)] = 0.5m,
            [(PokemonType.Grass, PokemonType.Fire)] = 0.5m,
            [(PokemonType.Grass, PokemonType.Water)] = 2m,
            [(PokemonType.Grass, PokemonType.Grass)] = 0.5m,
            [(PokemonType.Grass, PokemonType.Poison)] = 0.5m,
            [(PokemonType.Grass, PokemonType.Ground)] = 2m,
            [(PokemonType.Grass, PokemonType.Flying)] = 0.5m,
            [(PokemonType.Grass, PokemonType.Bug)] = 0.5m,
            [(PokemonType.Grass, PokemonType.Rock)] = 2m,
            [(PokemonType.Grass, PokemonType.Dragon)] = 0.5m,
            [(PokemonType.Grass, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Ice, PokemonType.Fire)] = 0.5m,
            [(PokemonType.Ice, PokemonType.Water)] = 0.5m,
            [(PokemonType.Ice, PokemonType.Grass)] = 2m,
            [(PokemonType.Ice, PokemonType.Ice)] = 0.5m,
            [(PokemonType.Ice, PokemonType.Ground)] = 2m,
            [(PokemonType.Ice, PokemonType.Flying)] = 2m,
            [(PokemonType.Ice, PokemonType.Dragon)] = 2m,
            [(PokemonType.Ice, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Fighting, PokemonType.Normal)] = 2m,
            [(PokemonType.Fighting, PokemonType.Ice)] = 2m,
            [(PokemonType.Fighting, PokemonType.Poison)] = 0.5m,
            [(PokemonType.Fighting, PokemonType.Flying)] = 0.5m,
            [(PokemonType.Fighting, PokemonType.Psychic)] = 0.5m,
            [(PokemonType.Fighting, PokemonType.Bug)] = 0.5m,
            [(PokemonType.Fighting, PokemonType.Rock)] = 2m,
            [(PokemonType.Fighting, PokemonType.Ghost)] = 0m,
            [(PokemonType.Fighting, PokemonType.Dark)] = 2m,
            [(PokemonType.Fighting, PokemonType.Steel)] = 2m,
            [(PokemonType.Fighting, PokemonType.Fairy)] = 0.5m,
            [(PokemonType.Poison, PokemonType.Grass)] = 2m,
            [(PokemonType.Poison, PokemonType.Poison)] = 0.5m,
            [(PokemonType.Poison, PokemonType.Ground)] = 0.5m,
            [(PokemonType.Poison, PokemonType.Rock)] = 0.5m,
            [(PokemonType.Poison, PokemonType.Ghost)] = 0.5m,
            [(PokemonType.Poison, PokemonType.Steel)] = 0m,
            [(PokemonType.Poison, PokemonType.Fairy)] = 2m,
            [(PokemonType.Ground, PokemonType.Fire)] = 2m,
            [(PokemonType.Ground, PokemonType.Electric)] = 2m,
            [(PokemonType.Ground, PokemonType.Grass)] = 0.5m,
            [(PokemonType.Ground, PokemonType.Poison)] = 2m,
            [(PokemonType.Ground, PokemonType.Flying)] = 0m,
            [(PokemonType.Ground, PokemonType.Bug)] = 0.5m,
            [(PokemonType.Ground, PokemonType.Rock)] = 2m,
            [(PokemonType.Ground, PokemonType.Steel)] = 2m,
            [(PokemonType.Flying, PokemonType.Electric)] = 0.5m,
            [(PokemonType.Flying, PokemonType.Grass)] = 2m,
            [(PokemonType.Flying, PokemonType.Fighting)] = 2m,
            [(PokemonType.Flying, PokemonType.Bug)] = 2m,
            [(PokemonType.Flying, PokemonType.Rock)] = 0.5m,
            [(PokemonType.Flying, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Psychic, PokemonType.Fighting)] = 2m,
            [(PokemonType.Psychic, PokemonType.Poison)] = 2m,
            [(PokemonType.Psychic, PokemonType.Psychic)] = 0.5m,
            [(PokemonType.Psychic, PokemonType.Dark)] = 0m,
            [(PokemonType.Psychic, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Bug, PokemonType.Fire)] = 0.5m,
            [(PokemonType.Bug, PokemonType.Grass)] = 2m,
            [(PokemonType.Bug, PokemonType.Fighting)] = 0.5m,
            [(PokemonType.Bug, PokemonType.Poison)] = 0.5m,
            [(PokemonType.Bug, PokemonType.Flying)] = 0.5m,
            [(PokemonType.Bug, PokemonType.Psychic)] = 2m,
            [(PokemonType.Bug, PokemonType.Ghost)] = 0.5m,
            [(PokemonType.Bug, PokemonType.Dark)] = 2m,
            [(PokemonType.Bug, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Bug, PokemonType.Fairy)] = 0.5m,
            [(PokemonType.Rock, PokemonType.Fire)] = 2m,
            [(PokemonType.Rock, PokemonType.Ice)] = 2m,
            [(PokemonType.Rock, PokemonType.Fighting)] = 0.5m,
            [(PokemonType.Rock, PokemonType.Ground)] = 0.5m,
            [(PokemonType.Rock, PokemonType.Flying)] = 2m,
            [(PokemonType.Rock, PokemonType.Bug)] = 2m,
            [(PokemonType.Rock, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Ghost, PokemonType.Normal)] = 0m,
            [(PokemonType.Ghost, PokemonType.Psychic)] = 2m,
            [(PokemonType.Ghost, PokemonType.Ghost)] = 2m,
            [(PokemonType.Ghost, PokemonType.Dark)] = 0.5m,
            [(PokemonType.Dragon, PokemonType.Dragon)] = 2m,
            [(PokemonType.Dragon, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Dragon, PokemonType.Fairy)] = 0m,
            [(PokemonType.Dark, PokemonType.Fighting)] = 0.5m,
            [(PokemonType.Dark, PokemonType.Psychic)] = 2m,
            [(PokemonType.Dark, PokemonType.Ghost)] = 2m,
            [(PokemonType.Dark, PokemonType.Dark)] = 0.5m,
            [(PokemonType.Dark, PokemonType.Fairy)] = 0.5m,
            [(PokemonType.Steel, PokemonType.Fire)] = 0.5m,
            [(PokemonType.Steel, PokemonType.Water)] = 0.5m,
            [(PokemonType.Steel, PokemonType.Electric)] = 0.5m,
            [(PokemonType.Steel, PokemonType.Ice)] = 2m,
            [(PokemonType.Steel, PokemonType.Rock)] = 2m,
            [(PokemonType.Steel, PokemonType.Steel)] = 0.5m,
            [(PokemonType.Steel, PokemonType.Fairy)] = 2m,
            [(PokemonType.Fairy, PokemonType.Fire)] = 0.5m,
            [(PokemonType.Fairy, PokemonType.Fighting)] = 2m,
            [(PokemonType.Fairy, PokemonType.Poison)] = 0.5m,
            [(PokemonType.Fairy, PokemonType.Dragon)] = 2m,
            [(PokemonType.Fairy, PokemonType.Dark)] = 2m,
            [(PokemonType.Fairy, PokemonType.Steel)] = 0.5m
        };

    /// <summary>タイプ相性倍率を取得します。</summary>
    /// <param name="moveType">攻撃技タイプです。</param>
    /// <param name="defendingTypes">防御側タイプです。</param>
    /// <returns>掛け合わせ後の倍率です。</returns>
    public static decimal GetEffectiveness(PokemonType moveType, PokemonTypeSlot defendingTypes)
    {
        var multiplier = GetSingleEffectiveness(moveType, defendingTypes.PrimaryType);
        if (defendingTypes.SecondaryType is { } secondaryType)
        {
            multiplier *= GetSingleEffectiveness(moveType, secondaryType);
        }

        return multiplier;
    }

    private static decimal GetSingleEffectiveness(PokemonType moveType, PokemonType defendingType)
        => Multipliers.TryGetValue((moveType, defendingType), out var multiplier) ? multiplier : 1m;
}
