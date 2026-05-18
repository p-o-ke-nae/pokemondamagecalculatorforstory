using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed record GetBattlesByRunQuery(Guid RunId) : IRequest<IReadOnlyList<BattleDto>>;
