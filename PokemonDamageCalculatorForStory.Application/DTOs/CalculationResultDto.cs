namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record CalculationResultDto(Guid Id, Guid RunId, Guid BattleId, string AttackerParams, string DefenderParams, IReadOnlyList<int> DamageRolls);
