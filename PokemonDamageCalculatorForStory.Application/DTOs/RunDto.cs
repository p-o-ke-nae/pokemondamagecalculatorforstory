namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record RunDto(Guid Id, string OwnerUserId, Guid RuleSetId, string Name, string Status);
