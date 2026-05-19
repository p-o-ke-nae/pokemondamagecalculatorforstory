namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

internal readonly record struct ParsedSnapshotValues(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed);
