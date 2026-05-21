using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Tests.Web.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Web.Controllers;

public sealed class AdminUserAuthorizationsAdminControllerTests
{
    [Fact]
    public async Task Administrator_Can_Create_UserAuthorization()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "admin-token");

        var response = await client.PostAsJsonAsync("/api/admin/user-authorizations", new AdminUserAuthorizationUpsertRequest("new-editor", AppRoles.MasterEditor, [AppPermissions.MastersView, AppPermissions.ManageRuleSets]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(factory.AuditLogger.Entries);
        Assert.Equal("Succeeded", factory.AuditLogger.Entries[0].Result);
    }

    [Fact]
    public async Task MasterEditor_Cannot_Access_UserAuthorization_Admin_Api()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "editor-token");

        var response = await client.GetAsync("/api/admin/user-authorizations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.AuditLogger.Entries);
    }

    [Fact]
    public async Task Unknown_Permission_Returns_400()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "admin-token");

        var response = await client.PostAsJsonAsync("/api/admin/user-authorizations", new AdminUserAuthorizationUpsertRequest("bad-editor", AppRoles.MasterEditor, ["unknown.permission"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.AuditLogger.Entries);
    }

    [Fact]
    public async Task Last_Administrator_Role_Change_Returns_409_And_Audit()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "admin-token");

        var response = await client.PutAsJsonAsync($"/api/admin/user-authorizations/{CustomWebApplicationFactory.AdminGoogleUserId}", new AdminUserAuthorizationUpsertRequest(CustomWebApplicationFactory.AdminGoogleUserId, AppRoles.Member, [AppPermissions.MastersView]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(factory.AuditLogger.Entries);
        Assert.Equal("Rejected", factory.AuditLogger.Entries[0].Result);
    }

    [Fact]
    public async Task Last_Administrator_Permissions_Only_Update_Succeeds()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "admin-token");

        var response = await client.PutAsJsonAsync($"/api/admin/user-authorizations/{CustomWebApplicationFactory.AdminGoogleUserId}", new AdminUserAuthorizationUpsertRequest(CustomWebApplicationFactory.AdminGoogleUserId, AppRoles.Administrator, [AppPermissions.MastersView]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(factory.AuditLogger.Entries);
        Assert.Equal("Succeeded", factory.AuditLogger.Entries[0].Result);
    }

    private static HttpClient CreateAuthorizedClient(CustomWebApplicationFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
