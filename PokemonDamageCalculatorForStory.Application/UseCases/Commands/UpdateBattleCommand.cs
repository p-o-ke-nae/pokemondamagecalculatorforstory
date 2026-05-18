using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record UpdateBattleCommand(Guid RunId, Guid Id, string EnemyPokemon, int Sequence) : IRequest<BattleDto?>;
