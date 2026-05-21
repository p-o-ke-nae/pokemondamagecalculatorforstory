using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Tests.Web.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Web.Controllers;

public sealed class AdminUiControllerTests
{
    [Fact]
    public async Task Login_Then_MasterEditor_Can_View_RuleSet_Page_But_Not_UserAuthorization_Page()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "editor-token", "/admin/masters/rule-sets");

        var ruleSetsPage = await client.GetAsync("/admin/masters/rule-sets");
        Assert.Equal(HttpStatusCode.OK, ruleSetsPage.StatusCode);
        Assert.Contains("RuleSet 管理", await ruleSetsPage.Content.ReadAsStringAsync());

        var userAuthorizationsPage = await client.GetAsync("/admin/masters/user-authorizations");
        Assert.Equal(HttpStatusCode.Redirect, userAuthorizationsPage.StatusCode);
        Assert.Contains("/admin/login", userAuthorizationsPage.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Administrator_Login_Can_View_UserAuthorization_Page()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "admin-token", "/admin/masters/user-authorizations");

        var page = await client.GetAsync("/admin/masters/user-authorizations");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("User Authorization 管理", await page.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Authorization_Uses_Current_Role_On_Each_Request_For_Promotion()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "editor-token", "/admin/masters/rule-sets");

        var beforePromotion = await client.GetAsync("/admin/masters/user-authorizations");
        Assert.Equal(HttpStatusCode.Redirect, beforePromotion.StatusCode);
        Assert.Contains("/admin/login", beforePromotion.Headers.Location?.OriginalString);

        await UpdateRoleAsync(factory, CustomWebApplicationFactory.MasterEditorGoogleUserId, AppRoles.Administrator);

        var afterPromotion = await client.GetAsync("/admin/masters/user-authorizations");
        Assert.Equal(HttpStatusCode.OK, afterPromotion.StatusCode);
        Assert.Contains("User Authorization 管理", await afterPromotion.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Authorization_Uses_Current_Role_On_Each_Request_For_Demotion()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "admin-token", "/admin/masters/user-authorizations");

        var beforeDemotion = await client.GetAsync("/admin/masters/user-authorizations");
        Assert.Equal(HttpStatusCode.OK, beforeDemotion.StatusCode);

        await UpdateRoleAsync(factory, CustomWebApplicationFactory.AdminGoogleUserId, AppRoles.Member);

        var afterDemotion = await client.GetAsync("/admin/masters/user-authorizations");
        Assert.Equal(HttpStatusCode.Redirect, afterDemotion.StatusCode);
        Assert.Contains("/admin/login", afterDemotion.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["accessToken"] = "admin-token",
            ["returnUrl"] = "/admin/masters/user-authorizations"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "admin-token");

        var response = await client.PostAsync("/admin/logout", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RuleSet_Create_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "editor-token");
        await GetAntiforgeryTokenAsync(client, "/admin/masters/rule-sets/new");

        var response = await client.PostAsync("/admin/masters/rule-sets/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Slug"] = "gen7-standard",
            ["Generation"] = "7",
            ["Title"] = "第7世代標準ルール",
            ["Version"] = "1.0",
            ["Status"] = "Active",
            ["Summary"] = "summary"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RuleSet_Update_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "editor-token");
        await GetAntiforgeryTokenAsync(client, $"/admin/masters/rule-sets/{CustomWebApplicationFactory.Gen6RuleSetId}");

        var response = await client.PostAsync($"/admin/masters/rule-sets/{CustomWebApplicationFactory.Gen6RuleSetId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Slug"] = "gen6-standard",
            ["Generation"] = "6",
            ["Title"] = "第6世代標準ルール",
            ["Version"] = "1.1",
            ["Status"] = "Active",
            ["Summary"] = "summary"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RuleSet_Delete_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "editor-token");
        await GetAntiforgeryTokenAsync(client, $"/admin/masters/rule-sets/{CustomWebApplicationFactory.DraftRuleSetId}");

        var response = await client.PostAsync($"/admin/masters/rule-sets/{CustomWebApplicationFactory.DraftRuleSetId}/delete", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserAuthorization_Create_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "admin-token");
        await GetAntiforgeryTokenAsync(client, "/admin/masters/user-authorizations/new");

        var response = await client.PostAsync("/admin/masters/user-authorizations/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["googleUserId"] = "new-editor",
            ["role"] = AppRoles.MasterEditor,
            ["permissions"] = AppPermissions.ManageRuleSets
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserAuthorization_Update_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "admin-token");
        await GetAntiforgeryTokenAsync(client, $"/admin/masters/user-authorizations/{CustomWebApplicationFactory.MasterEditorGoogleUserId}");

        var response = await client.PostAsync($"/admin/masters/user-authorizations/{CustomWebApplicationFactory.MasterEditorGoogleUserId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["role"] = AppRoles.MasterEditor,
            ["permissions"] = AppPermissions.ManageRuleSets
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserAuthorization_Delete_Post_Without_Antiforgery_Token_Returns_BadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        await LoginAsync(client, "admin-token");
        await GetAntiforgeryTokenAsync(client, $"/admin/masters/user-authorizations/{CustomWebApplicationFactory.MasterEditorGoogleUserId}");

        var response = await client.PostAsync($"/admin/masters/user-authorizations/{CustomWebApplicationFactory.MasterEditorGoogleUserId}/delete", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task LoginAsync(HttpClient client, string accessToken, string returnUrl = "/admin/masters/rule-sets")
    {
        var antiForgeryToken = await GetAntiforgeryTokenAsync(client, "/admin/login");

        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiForgeryToken,
            ["accessToken"] = accessToken,
            ["returnUrl"] = returnUrl
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase);

        Assert.True(match.Success, $"Antiforgery token was not found in '{path}'.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task UpdateRoleAsync(CustomWebApplicationFactory factory, string googleUserId, string role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userAuthorization = await dbContext.PersistedUserAuthorizationInfos.FindAsync(googleUserId);

        Assert.NotNull(userAuthorization);

        userAuthorization!.Role = role;
        await dbContext.SaveChangesAsync();
    }
}
