using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record UpdateRunCommand(Guid Id, string Name, string Status) : IRequest<RunDto?>;
