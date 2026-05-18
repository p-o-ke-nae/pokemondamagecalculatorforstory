using MediatR;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed class DeleteBattleCommandHandler(IBattleRepository repository) : IRequestHandler<DeleteBattleCommand, bool>
{
    public Task<bool> Handle(DeleteBattleCommand request, CancellationToken cancellationToken)
        => repository.DeleteAsync(request.RunId, request.Id, cancellationToken);
}
