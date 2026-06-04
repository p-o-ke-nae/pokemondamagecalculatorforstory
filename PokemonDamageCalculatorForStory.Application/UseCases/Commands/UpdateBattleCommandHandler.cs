using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class UpdateBattleCommandHandler(IBattleRepository repository) : IRequestHandler<UpdateBattleCommand, BattleDto?>
{
    public async Task<BattleDto?> Handle(UpdateBattleCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindAsync(request.RunId, request.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = Battle.Restore(existing.Id, existing.RunId, request.EnemyPokemon, request.Sequence);
        var saved = await repository.SaveAsync(updated, cancellationToken);
        return new BattleDto(saved.Id, saved.RunId, saved.EnemyPokemon, saved.Sequence);
    }
}
