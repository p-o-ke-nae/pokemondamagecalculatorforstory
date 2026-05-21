using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Extensions;

namespace PokemonDamageCalculatorForStory.Authentication;

public sealed class UserAuthorizationClaimsTransformation(IUserAuthorizationInfoRepository repository) : IClaimsTransformation
{
    private const string PermissionClaimType = "permission";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var googleUserId = principal.GetGoogleUserIdOrNull();
        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            return principal;
        }

        var userAuthorization = await repository.FindByGoogleUserIdAsync(googleUserId);
        var transformedPrincipal = CreatePrincipalWithoutAuthorizationClaims(principal);
        var identity = new ClaimsIdentity(authenticationType: nameof(UserAuthorizationClaimsTransformation));
        identity.AddClaim(new Claim(ClaimTypes.Role, userAuthorization?.Role ?? AppRoles.Member));

        foreach (var permission in userAuthorization?.Permissions ?? [])
        {
            identity.AddClaim(new Claim(PermissionClaimType, permission));
        }

        transformedPrincipal.AddIdentity(identity);
        return transformedPrincipal;
    }

    private static ClaimsPrincipal CreatePrincipalWithoutAuthorizationClaims(ClaimsPrincipal principal)
    {
        return new ClaimsPrincipal(principal.Identities.Select(identity =>
        {
            var sanitizedIdentity = new ClaimsIdentity(
                identity.Claims.Where(claim => claim.Type is not ClaimTypes.Role && claim.Type is not PermissionClaimType),
                identity.AuthenticationType,
                identity.NameClaimType,
                identity.RoleClaimType);

            sanitizedIdentity.Actor = identity.Actor;
            sanitizedIdentity.BootstrapContext = identity.BootstrapContext;
            sanitizedIdentity.Label = identity.Label;

            return sanitizedIdentity;
        }));
    }
}
