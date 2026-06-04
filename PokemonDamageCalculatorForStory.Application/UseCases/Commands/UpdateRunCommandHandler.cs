using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class UpdateRunCommandHandler(IRunRepository repository) : IRequestHandler<UpdateRunCommand, RunDto?>
{
    public async Task<RunDto?> Handle(UpdateRunCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = Domain.Entities.Run.Restore(existing.Id, existing.OwnerUserId, existing.RuleSetId, request.Name, request.Status);
        var saved = await repository.SaveAsync(updated, cancellationToken);
        return new RunDto(saved.Id, saved.OwnerUserId, saved.RuleSetId, saved.Name, saved.Status);
    }
}
