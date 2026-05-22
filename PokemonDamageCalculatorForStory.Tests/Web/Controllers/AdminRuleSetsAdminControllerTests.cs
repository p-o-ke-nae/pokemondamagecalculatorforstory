using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Tests.Web.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Web.Controllers;

public sealed class AdminRuleSetsAdminControllerTests
{
    [Fact]
    public async Task MasterEditor_Can_Create_RuleSet_And_Audit_Is_Recorded()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "editor-token");

        var response = await client.PostAsJsonAsync("/api/admin/rule-sets", new AdminRuleSetUpsertRequest("gen7-standard", 7, "第7世代標準ルール", "1.0", "Active", "summary"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(factory.AuditLogger.Entries);
        Assert.Equal("Succeeded", factory.AuditLogger.Entries[0].Result);
        Assert.Equal(CustomWebApplicationFactory.MasterEditorGoogleUserId, factory.AuditLogger.Entries[0].ActorGoogleUserId);
    }

    [Fact]
    public async Task Member_Cannot_Access_Admin_RuleSets_Api()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "member-token");

        var response = await client.GetAsync("/api/admin/rule-sets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.AuditLogger.Entries);
    }

    [Fact]
    public async Task Anonymous_Admin_RuleSets_Api_Returns_401()
    {
        using var factory = new CustomWebApplicationFactory();
        var response = await factory.CreateClient().GetAsync("/api/admin/rule-sets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.AuditLogger.Entries);
    }

    [Fact]
    public async Task Duplicate_Slug_Returns_409_And_Rejected_Audit()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "admin-token");

        var response = await client.PostAsJsonAsync("/api/admin/rule-sets", new AdminRuleSetUpsertRequest("gen6-standard", 6, "dup", "1.0", "Active", ""));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(factory.AuditLogger.Entries);
        Assert.Equal("Rejected", factory.AuditLogger.Entries[0].Result);
    }

    [Fact]
    public async Task Referenced_RuleSet_Delete_Returns_409_And_Rejected_Audit()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "admin-token");

        var response = await client.DeleteAsync($"/api/admin/rule-sets/{CustomWebApplicationFactory.Gen6RuleSetId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(factory.AuditLogger.Entries);
        Assert.Equal("Rejected", factory.AuditLogger.Entries[0].Result);
    }

    [Fact]
    public async Task GetAll_Returns_Referenced_Flag()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthorizedClient(factory, "admin-token");

        var response = await client.GetAsync("/api/admin/rule-sets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var referenced = doc.RootElement.EnumerateArray().First(x => x.GetProperty("id").GetGuid() == CustomWebApplicationFactory.Gen6RuleSetId);
        Assert.True(referenced.GetProperty("isReferencedByRuns").GetBoolean());
    }

    private static HttpClient CreateAuthorizedClient(CustomWebApplicationFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
