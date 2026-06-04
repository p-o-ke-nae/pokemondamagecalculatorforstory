using MediatR;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record DeleteRunCommand(Guid Id) : IRequest<bool>;
