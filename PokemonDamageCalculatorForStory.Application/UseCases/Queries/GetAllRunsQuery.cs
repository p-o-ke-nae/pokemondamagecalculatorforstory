using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed record GetAllRunsQuery : IRequest<IReadOnlyList<RunDto>>;
