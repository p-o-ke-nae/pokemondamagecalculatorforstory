using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Domain.Ports;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PokemonDamageCalculatorForStory.Authentication;

public class GoogleAccessTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IGoogleAccessTokenValidationService _googleAccessTokenValidationService;
    private readonly IUserAuthorizationInfoRepository _userAuthorizationInfoRepository;

    public GoogleAccessTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IGoogleAccessTokenValidationService googleAccessTokenValidationService,
        IUserAuthorizationInfoRepository userAuthorizationInfoRepository)
        : base(options, logger, encoder)
    {
        _googleAccessTokenValidationService = googleAccessTokenValidationService;
        _userAuthorizationInfoRepository = userAuthorizationInfoRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse(authorizationHeaderValues, out var authorizationHeader)
            || !string.Equals(authorizationHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorizationHeader.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        var validationResult = await _googleAccessTokenValidationService.ValidateAsync(
            authorizationHeader.Parameter,
            Context.RequestAborted);

        if (!validationResult.IsValid)
        {
            return AuthenticateResult.Fail(validationResult.FailureReason ?? "Invalid Google access token.");
        }

        var claims = new List<Claim>
        {
            new Claim(GoogleClaimTypes.GoogleUserId, validationResult.GoogleUserId!),
            new Claim(ClaimTypes.NameIdentifier, validationResult.GoogleUserId!),
            new Claim(ClaimTypes.Email, validationResult.Email!),
            new Claim(ClaimTypes.Name, validationResult.Name!)
        };

        var authorizationInfo = await _userAuthorizationInfoRepository.FindByGoogleUserIdAsync(
            validationResult.GoogleUserId!,
            Context.RequestAborted);
        if (authorizationInfo is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, authorizationInfo.Role));
            claims.AddRange(authorizationInfo.Permissions.Select(permission => new Claim("permission", permission)));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, AppRoles.Member));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
