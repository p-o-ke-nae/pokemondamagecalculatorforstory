using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PokemonDamageCalculatorForStory.Authentication;

public class GoogleAccessTokenValidationService : IGoogleAccessTokenValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleAccessTokenValidationService> _logger;
    private readonly GoogleAuthenticationOptions _options;

    public GoogleAccessTokenValidationService(
        HttpClient httpClient,
        IOptions<GoogleAuthenticationOptions> options,
        ILogger<GoogleAccessTokenValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<GoogleAccessTokenValidationResult> ValidateAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            return GoogleAccessTokenValidationResult.Invalid("Authentication:Google:ClientId is not configured.");
        }

        try
        {
            var tokenInfo = await getTokenInfoAsync(accessToken, cancellationToken);
            if (tokenInfo == null)
            {
                return GoogleAccessTokenValidationResult.Invalid("Google token validation failed.");
            }

            if (!string.Equals(tokenInfo.Audience, _options.ClientId, StringComparison.Ordinal))
            {
                return GoogleAccessTokenValidationResult.Invalid("Google token audience does not match the configured client id.");
            }

            if (!isActive(tokenInfo.ExpiresIn))
            {
                return GoogleAccessTokenValidationResult.Invalid("Google access token has expired.");
            }

            var userInfo = await tryGetUserInfoAsync(accessToken, cancellationToken);
            var googleUserId = userInfo?.Subject ?? tokenInfo.Subject;
            var email = userInfo?.Email ?? tokenInfo.Email;
            var name = userInfo?.Name ?? tokenInfo.Name ?? tokenInfo.Email;

            if (string.IsNullOrWhiteSpace(googleUserId)
                || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(name))
            {
                return GoogleAccessTokenValidationResult.Invalid("Google token response did not include the required user identity fields.");
            }

            return GoogleAccessTokenValidationResult.Valid(googleUserId, email, name);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to validate Google access token.");
            return GoogleAccessTokenValidationResult.Invalid("Failed to contact Google's token validation service.");
        }
    }

    private async Task<GoogleTokenInfoResponse?> getTokenInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var endpoint = $"{_options.TokenInfoEndpoint}?access_token={Uri.EscapeDataString(accessToken)}";
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GoogleTokenInfoResponse>(cancellationToken: cancellationToken);
    }

    private async Task<GoogleUserInfoResponse?> tryGetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(cancellationToken: cancellationToken);
    }

    private static bool isActive(string? expiresIn)
    {
        return int.TryParse(expiresIn, out var secondsRemaining) && secondsRemaining > 0;
    }

    private sealed class GoogleTokenInfoResponse
    {
        [JsonPropertyName("aud")]
        public string? Audience { get; init; }

        [JsonPropertyName("sub")]
        public string? Subject { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("expires_in")]
        public string? ExpiresIn { get; init; }
    }

    private sealed class GoogleUserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string? Subject { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}