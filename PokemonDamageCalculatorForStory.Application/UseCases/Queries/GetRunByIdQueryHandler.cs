using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed class GetRunByIdQueryHandler(IRunRepository repository) : IRequestHandler<GetRunByIdQuery, RunDto?>
{
    public async Task<RunDto?> Handle(GetRunByIdQuery request, CancellationToken cancellationToken)
    {
        var run = await repository.FindByIdAsync(request.Id, cancellationToken);
        return run is null ? null : new RunDto(run.Id, run.OwnerUserId, run.RuleSetId, run.Name, run.Status);
    }
}
