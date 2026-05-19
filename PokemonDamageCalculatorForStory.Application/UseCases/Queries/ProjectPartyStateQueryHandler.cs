using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed class ProjectPartyStateQueryHandler(IBattleRepository repository) : IRequestHandler<ProjectPartyStateQuery, PartyStateDto>
{
    public async Task<PartyStateDto> Handle(ProjectPartyStateQuery request, CancellationToken cancellationToken)
    {
        var snapshots = await repository.ListSnapshotsByRunAsync(request.RunId, cancellationToken);
        var dtos = snapshots
            .Select(snapshot => new OwnPokemonSnapshotDto(snapshot.Id, snapshot.BattleId, snapshot.Species, snapshot.Level, snapshot.BaseStats?.ToJson(), snapshot.IVs?.ToJson(), snapshot.Stats.ToJson(), snapshot.EVs.ToJson()))
            .ToList()
            .AsReadOnly();
        return new PartyStateDto(request.RunId, dtos);
    }
}
