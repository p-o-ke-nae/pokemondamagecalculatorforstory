namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record RuleSetDto(Guid Id, string Slug, int Generation, string Title, string Version, string Status, string Summary);
