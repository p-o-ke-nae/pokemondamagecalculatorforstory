namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record BattleDto(Guid Id, Guid RunId, string EnemyPokemon, int Sequence);
