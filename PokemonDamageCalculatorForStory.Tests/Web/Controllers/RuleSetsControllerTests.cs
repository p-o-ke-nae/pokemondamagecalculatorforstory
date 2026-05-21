using System.Net;
using System.Text.Json;
using PokemonDamageCalculatorForStory.Tests.Web.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Web.Controllers;

public sealed class RuleSetsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RuleSetsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_Returns_200_With_Seeded_RuleSet()
    {
        var response = await _client.GetAsync("/api/rule-sets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
        Assert.Equal("gen6-standard", doc.RootElement[0].GetProperty("slug").GetString());
    }

    [Fact]
    public async Task GetById_Returns_200_For_Existing_RuleSet()
    {
        var response = await _client.GetAsync($"/api/rule-sets/{CustomWebApplicationFactory.Gen6RuleSetId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("gen6-standard", doc.RootElement.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task GetById_Returns_404_For_Unknown_RuleSet()
    {
        var response = await _client.GetAsync($"/api/rule-sets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000002")]
    [InlineData("00000000-0000-0000-0000-000000000003")]
    public async Task GetById_Returns_404_For_NonActive_RuleSet(string id)
    {
        var response = await _client.GetAsync($"/api/rule-sets/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_Returns_200()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
