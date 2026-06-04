namespace PokemonDamageCalculatorForStory.Application.DTOs.Admin;

public sealed record AdminUserAuthorizationUpsertRequest(
    string GoogleUserId,
    string Role,
    IReadOnlyList<string> Permissions);
