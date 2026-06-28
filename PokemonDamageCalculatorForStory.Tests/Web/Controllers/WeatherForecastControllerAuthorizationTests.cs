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

    [Fact(DisplayName = "Program_ConfigureSwagger_保護対象エンドポイントにGoogleBearerセキュリティを定義する")]
    public async Task Program_ConfigureSwagger_DefinesGoogleBearerSecurityForProtectedEndpoints()
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

    [Fact(DisplayName = "WeatherForecastController_Create_認証トークンなしの作成要求で401を返す")]
    public async Task WeatherForecastController_Create_Returns401WithoutToken()
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

    [Fact(DisplayName = "WeatherForecastController_Update_権限のない非所有者の更新要求で403を返す")]
    public async Task WeatherForecastController_Update_Returns403ForNonOwnerWithoutPermission()
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

    [Fact(DisplayName = "WeatherForecastController_Update_所有者の更新要求で200を返す")]
    public async Task WeatherForecastController_Update_Returns200ForOwner()
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

    [Fact(DisplayName = "WeatherForecastController_GetById_匿名ユーザーの公開予報取得で200を返す")]
    public async Task WeatherForecastController_GetById_Returns200ForAnonymousPublicRequest()
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