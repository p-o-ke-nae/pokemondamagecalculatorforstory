using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed class GetBattlesByRunQueryHandler(IBattleRepository repository) : IRequestHandler<GetBattlesByRunQuery, IReadOnlyList<BattleDto>>
{
    public async Task<IReadOnlyList<BattleDto>> Handle(GetBattlesByRunQuery request, CancellationToken cancellationToken)
    {
        var battles = await repository.ListByRunAsync(request.RunId, cancellationToken);
        return battles.Select(battle => new BattleDto(battle.Id, battle.RunId, battle.EnemyPokemon, battle.Sequence)).ToList().AsReadOnly();
    }
}
