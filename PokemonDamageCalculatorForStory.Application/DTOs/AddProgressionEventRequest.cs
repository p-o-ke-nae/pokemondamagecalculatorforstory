namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record AddProgressionEventRequest(Guid BattleId, string Species, int Level, string Stats, string EVs);
