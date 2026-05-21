using System.Net;
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

        var loginGet = await client.GetAsync("/admin/login");
        Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);

        var loginResponse = await client.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["accessToken"] = "editor-token",
            ["returnUrl"] = "/admin/masters/rule-sets"
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

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

        var loginResponse = await client.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["accessToken"] = "admin-token",
            ["returnUrl"] = "/admin/masters/user-authorizations"
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        var page = await client.GetAsync("/admin/masters/user-authorizations");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("User Authorization 管理", await page.Content.ReadAsStringAsync());
    }
}
