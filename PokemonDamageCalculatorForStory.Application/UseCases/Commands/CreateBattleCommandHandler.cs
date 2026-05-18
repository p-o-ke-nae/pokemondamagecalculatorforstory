using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class CreateBattleCommandHandler(IBattleRepository repository) : IRequestHandler<CreateBattleCommand, BattleDto>
{
    public async Task<BattleDto> Handle(CreateBattleCommand request, CancellationToken cancellationToken)
    {
        var battle = Battle.Create(request.RunId, request.EnemyPokemon, request.Sequence);
        var saved = await repository.SaveAsync(battle, cancellationToken);
        return new BattleDto(saved.Id, saved.RunId, saved.EnemyPokemon, saved.Sequence);
    }
}
