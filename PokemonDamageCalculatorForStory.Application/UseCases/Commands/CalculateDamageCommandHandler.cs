using System.Text.Json;
using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class CalculateDamageCommandHandler(IBattleRepository repository) : IRequestHandler<CalculateDamageCommand, CalculationResultDto>
{
    public async Task<CalculationResultDto> Handle(CalculateDamageCommand request, CancellationToken cancellationToken)
    {
        var levelFactor = (2 * request.AttackerLevel / 5) + 2;
        var baseDamage = (int)Math.Floor((double)(levelFactor * request.MovePower * request.AttackStat) / request.DefenseStat / 50) + 2;

        var stab = request.HasStab ? 1.5f : 1.0f;
        var modifier = stab * request.TypeEffectiveness;

        var rolls = Enumerable.Range(85, 16)
            .Select(roll => (int)Math.Floor(baseDamage * modifier * roll / 100.0))
            .ToArray();

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
