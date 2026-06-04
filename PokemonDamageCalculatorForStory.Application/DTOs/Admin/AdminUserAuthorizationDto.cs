namespace PokemonDamageCalculatorForStory.Application.DTOs.Admin;

public sealed record AdminUserAuthorizationDto(
    string GoogleUserId,
    string Role,
    IReadOnlyList<string> Permissions,
    bool IsLastAdministrator);
