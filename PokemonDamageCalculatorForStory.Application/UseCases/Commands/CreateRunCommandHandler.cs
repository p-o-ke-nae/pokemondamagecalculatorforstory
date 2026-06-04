using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class CreateRunCommandHandler(IRunRepository repository) : IRequestHandler<CreateRunCommand, RunDto>
{
    public async Task<RunDto> Handle(CreateRunCommand request, CancellationToken cancellationToken)
    {
        var run = Run.Create(request.OwnerUserId, request.RuleSetId, request.Name, "Active");
        var saved = await repository.SaveAsync(run, cancellationToken);
        return new RunDto(saved.Id, saved.OwnerUserId, saved.RuleSetId, saved.Name, saved.Status);
    }
}
