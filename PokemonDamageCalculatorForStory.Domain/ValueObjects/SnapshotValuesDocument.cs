namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

internal sealed record SnapshotValuesDocument(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed);
