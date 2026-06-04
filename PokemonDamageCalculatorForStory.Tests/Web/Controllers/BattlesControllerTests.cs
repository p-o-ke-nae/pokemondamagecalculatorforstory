using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PokemonDamageCalculatorForStory.Tests.Web.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Web.Controllers;

public sealed class BattlesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public BattlesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Calculate_With_InvalidLevel_Returns_400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/runs/{Guid.NewGuid()}/battles/{Guid.NewGuid()}/calculate")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new
                    {
                        attackerLevel = 0,
                        attackStat = 100,
                        movePower = 80,
                        isSpecialMove = false,
                        hasStab = false,
                        defenseStat = 100,
                        typeEffectiveness = 1.0f
                    },
                    JsonOptions),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "owner-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
