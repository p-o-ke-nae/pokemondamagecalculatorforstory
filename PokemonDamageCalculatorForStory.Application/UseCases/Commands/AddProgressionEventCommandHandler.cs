using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class AddProgressionEventCommandHandler(IBattleRepository repository) : IRequestHandler<AddProgressionEventCommand, OwnPokemonSnapshotDto>
{
    public async Task<OwnPokemonSnapshotDto> Handle(AddProgressionEventCommand request, CancellationToken cancellationToken)
    {
        var battle = await repository.FindAsync(request.RunId, request.BattleId, cancellationToken);
        if (battle is null)
            throw new NotFoundException($"Battle '{request.BattleId}' not found for run '{request.RunId}'.");

        var snapshot = OwnPokemonSnapshot.Create(
            request.BattleId,
            request.Species,
            request.Level,
            SpeciesBaseStats.FromJson(request.BaseStats),
            IndividualValues.FromJson(request.IVs),
            PokemonStats.FromJson(request.Stats),
            EffortValues.FromJson(request.EVs));
        var saved = await repository.SaveSnapshotAsync(snapshot, request.RunId, cancellationToken);
        return new OwnPokemonSnapshotDto(saved.Id, saved.BattleId, saved.Species, saved.Level, saved.BaseStats?.ToJson(), saved.IVs?.ToJson(), saved.Stats.ToJson(), saved.EVs.ToJson());
    }
}
