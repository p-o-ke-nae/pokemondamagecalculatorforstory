using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record CreateRunCommand(string OwnerUserId, string Name, Guid RuleSetId) : IRequest<RunDto>;
