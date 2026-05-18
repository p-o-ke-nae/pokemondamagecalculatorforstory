using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record AddProgressionEventCommand(Guid RunId, Guid BattleId, string Species, int Level, string Stats, string EVs) : IRequest<OwnPokemonSnapshotDto>;
