using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;
using PokemonDamageCalculatorForStory.Application.Validators;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Application.Validators;

public sealed class AdminCommandValidatorsTests
{
    [Fact]
    public void UserAuthorizationValidator_Rejects_Unknown_Permission()
    {
        var validator = new CreateAdminUserAuthorizationCommandValidator();
        var command = new CreateAdminUserAuthorizationCommand("admin-1", AppRoles.Administrator, "editor-1", AppRoles.MasterEditor, ["unknown.permission"]);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains("Permissions"));
    }

    [Fact]
    public void UserAuthorizationValidator_Allows_Catalog_Permissions()
    {
        var validator = new CreateAdminUserAuthorizationCommandValidator();
        var command = new CreateAdminUserAuthorizationCommand("admin-1", AppRoles.Administrator, "editor-1", AppRoles.MasterEditor, [AppPermissions.MastersView, AppPermissions.ManageRuleSets]);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
