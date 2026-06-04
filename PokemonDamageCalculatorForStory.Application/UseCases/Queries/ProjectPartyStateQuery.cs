using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed record ProjectPartyStateQuery(Guid RunId) : IRequest<PartyStateDto>;
