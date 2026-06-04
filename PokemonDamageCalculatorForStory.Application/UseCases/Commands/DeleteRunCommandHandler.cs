using MediatR;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class DeleteRunCommandHandler(IRunRepository repository) : IRequestHandler<DeleteRunCommand, bool>
{
    public Task<bool> Handle(DeleteRunCommand request, CancellationToken cancellationToken)
        => repository.DeleteAsync(request.Id, cancellationToken);
}
