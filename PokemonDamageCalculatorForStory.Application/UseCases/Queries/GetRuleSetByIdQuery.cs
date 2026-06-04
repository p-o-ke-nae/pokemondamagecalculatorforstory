using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed record GetRuleSetByIdQuery(Guid Id) : IRequest<RuleSetDto?>;
