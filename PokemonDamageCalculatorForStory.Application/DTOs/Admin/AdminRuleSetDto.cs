namespace PokemonDamageCalculatorForStory.Application.DTOs.Admin;

public sealed record AdminRuleSetDto(
    Guid Id,
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary,
    bool IsReferencedByRuns);
