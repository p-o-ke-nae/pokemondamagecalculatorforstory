namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record CalculateDamageRequest(int AttackerLevel, int AttackStat, int MovePower, bool IsSpecialMove, bool HasStab, int DefenseStat, float TypeEffectiveness);
