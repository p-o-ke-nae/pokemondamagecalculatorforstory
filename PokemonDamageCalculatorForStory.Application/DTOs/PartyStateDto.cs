namespace PokemonDamageCalculatorForStory.Application.DTOs;

public sealed record PartyStateDto(Guid RunId, IReadOnlyList<OwnPokemonSnapshotDto> Pokemon);
