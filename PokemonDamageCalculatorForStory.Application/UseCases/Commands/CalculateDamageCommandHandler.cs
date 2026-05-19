using System.Text.Json;
using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Calculations;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class CalculateDamageCommandHandler(IBattleRepository repository) : IRequestHandler<CalculateDamageCommand, CalculationResultDto>
{
    public async Task<CalculationResultDto> Handle(CalculateDamageCommand request, CancellationToken cancellationToken)
    {
        var rolls = PokemonDamageFormula.CalculateRolls(
            PokemonLevel.Create(request.AttackerLevel),
            AttackStat.Create(request.AttackStat),
            MovePower.Create(request.MovePower),
            request.HasStab,
            DefenseStat.Create(request.DefenseStat),
            TypeEffectivenessMultiplier.Create(request.TypeEffectiveness));

        var attackerParamsJson = JsonSerializer.Serialize(new
        {
            request.AttackerLevel,
            request.AttackStat,
            request.MovePower,
            request.IsSpecialMove,
            request.HasStab
        });
        var defenderParamsJson = JsonSerializer.Serialize(new
        {
            request.DefenseStat,
            request.TypeEffectiveness
        });

        var result = CalculationResult.Create(request.RunId, request.BattleId, attackerParamsJson, defenderParamsJson, rolls);
        var saved = await repository.SaveCalculationResultAsync(result, cancellationToken);
        return new CalculationResultDto(saved.Id, saved.RunId, saved.BattleId, saved.AttackerParams, saved.DefenderParams, saved.DamageRolls);
    }
}
