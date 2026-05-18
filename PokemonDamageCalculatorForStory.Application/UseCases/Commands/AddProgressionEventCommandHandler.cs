using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class AddProgressionEventCommandHandler(IBattleRepository repository) : IRequestHandler<AddProgressionEventCommand, OwnPokemonSnapshotDto>
{
    public async Task<OwnPokemonSnapshotDto> Handle(AddProgressionEventCommand request, CancellationToken cancellationToken)
    {
        var battle = await repository.FindAsync(request.RunId, request.BattleId, cancellationToken);
        if (battle is null)
            throw new NotFoundException($"Battle '{request.BattleId}' not found for run '{request.RunId}'.");

        var snapshot = OwnPokemonSnapshot.Create(request.BattleId, request.Species, request.Level, request.Stats, request.EVs);
        var saved = await repository.SaveSnapshotAsync(snapshot, request.RunId, cancellationToken);
        return new OwnPokemonSnapshotDto(saved.Id, saved.BattleId, saved.Species, saved.Level, saved.Stats, saved.EVs);
    }
}
