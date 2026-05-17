using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PokemonDamageCalculatorForStory.Tests.Web.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Web.Controllers;

public class WeatherForecastControllerAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public WeatherForecastControllerAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_Defines_GoogleBearer_Security_For_Protected_Endpoints()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var securityScheme = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("GoogleBearer");

        Assert.Equal("http", securityScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", securityScheme.GetProperty("scheme").GetString());

        var postOperation = root
            .GetProperty("paths")
            .GetProperty("/WeatherForecast")
            .GetProperty("post");

        Assert.True(postOperation.TryGetProperty("security", out var security));
        Assert.True(security.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Create_Without_Token_Returns_401()
    {
        using var response = await _client.PostAsync(
            "/WeatherForecast",
            CreateJsonContent(new
            {
                date = "2026-03-25",
                temperatureC = 20,
                summary = "Unauthorized create",
                isPublic = false
            }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_By_NonOwner_Without_Permission_Returns_403()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/WeatherForecast/{CustomWebApplicationFactory.PrivateForecastId}")
        {
            Content = CreateJsonContent(new
            {
                date = "2026-03-26",
                temperatureC = 19,
                summary = "Forbidden update",
                isPublic = false
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "other-token");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_By_Owner_Returns_200()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/WeatherForecast/{CustomWebApplicationFactory.PrivateForecastId}")
        {
            Content = CreateJsonContent(new
            {
                date = "2026-03-27",
                temperatureC = 17,
                summary = "Owner update",
                isPublic = false
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "owner-token");

        using var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Owner update", responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_Get_By_Anonymous_Returns_200()
    {
        using var response = await _client.GetAsync($"/WeatherForecast/{CustomWebApplicationFactory.PublicForecastId}");
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Public forecast", responseBody, StringComparison.Ordinal);
    }

    private static StringContent CreateJsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value, JsonSerializerOptions), Encoding.UTF8, "application/json");
    }
}