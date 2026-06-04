namespace PokemonDamageCalculatorForStory.Application.DTOs.Admin;

public sealed record AdminRuleSetUpsertRequest(
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary);
