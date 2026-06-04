using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record CalculateDamageCommand(Guid RunId, Guid BattleId, int AttackerLevel, int AttackStat, int MovePower, bool IsSpecialMove, bool HasStab, int DefenseStat, float TypeEffectiveness) : IRequest<CalculationResultDto>;
