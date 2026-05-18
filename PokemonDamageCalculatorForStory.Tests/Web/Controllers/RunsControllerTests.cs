using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PokemonDamageCalculatorForStory.Tests.Web.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Web.Controllers;

public sealed class RunsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public RunsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_Returns_200()
    {
        var response = await _client.GetAsync("/api/runs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_Without_Token_Returns_401()
    {
        var content = new StringContent(JsonSerializer.Serialize(new { name = "Test Run", ruleSetId = CustomWebApplicationFactory.Gen6RuleSetId }, JsonOptions), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/runs", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Token_Returns_201()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/runs")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { name = "My Run", ruleSetId = CustomWebApplicationFactory.Gen6RuleSetId }, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "owner-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
