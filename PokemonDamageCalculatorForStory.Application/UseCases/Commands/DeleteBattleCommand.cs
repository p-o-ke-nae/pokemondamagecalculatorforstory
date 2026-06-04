using MediatR;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record DeleteBattleCommand(Guid RunId, Guid Id) : IRequest<bool>;
