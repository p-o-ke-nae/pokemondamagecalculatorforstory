using PokemonDamageCalculatorForStory.Application.DTOs.Admin;

namespace PokemonDamageCalculatorForStory.ViewModels.Admin;

public sealed class AdminUserAuthorizationEditorViewModel
{
    public string? GoogleUserId { get; init; }
    public string Heading { get; init; } = string.Empty;
    public AdminUserAuthorizationUpsertRequest Form { get; init; } = new("", "", []);
    public bool IsLastAdministrator { get; init; }
    public IReadOnlyList<string> RoleOptions { get; init; } = [];
    public IReadOnlyList<string> PermissionOptions { get; init; } = [];
}
