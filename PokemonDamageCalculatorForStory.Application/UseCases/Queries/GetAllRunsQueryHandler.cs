using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed class GetAllRunsQueryHandler(IRunRepository repository) : IRequestHandler<GetAllRunsQuery, IReadOnlyList<RunDto>>
{
    public async Task<IReadOnlyList<RunDto>> Handle(GetAllRunsQuery request, CancellationToken cancellationToken)
    {
        var runs = await repository.FindAllAsync(cancellationToken);
        return runs.Select(run => new RunDto(run.Id, run.OwnerUserId, run.RuleSetId, run.Name, run.Status)).ToList().AsReadOnly();
    }
}
