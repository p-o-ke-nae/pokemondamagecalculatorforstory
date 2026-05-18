using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public sealed record CreateBattleCommand(Guid RunId, string EnemyPokemon, int Sequence) : IRequest<BattleDto>;
