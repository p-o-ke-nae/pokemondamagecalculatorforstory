using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Extensions;

namespace PokemonDamageCalculatorForStory.Authentication;

public sealed class UserAuthorizationClaimsTransformation(IUserAuthorizationInfoRepository repository) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        if (principal.Claims.Any(claim => claim.Type == ClaimTypes.Role))
        {
            return principal;
        }

        var googleUserId = principal.GetGoogleUserIdOrNull();
        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            return principal;
        }

        var userAuthorization = await repository.FindByGoogleUserIdAsync(googleUserId);
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Role, userAuthorization?.Role ?? AppRoles.Member));

        foreach (var permission in userAuthorization?.Permissions ?? [])
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        principal.AddIdentity(identity);
        return principal;
    }
}
