using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed record GetAllRuleSetsQuery : IRequest<IReadOnlyList<RuleSetDto>>;
