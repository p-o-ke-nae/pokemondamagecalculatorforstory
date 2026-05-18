namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record OwnPokemonSnapshotDto(Guid Id, Guid BattleId, string Species, int Level, string Stats, string EVs);
