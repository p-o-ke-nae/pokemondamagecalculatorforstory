using System.Security.Claims;
using PokenaeTemplate.Authentication;

namespace PokenaeTemplate.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetGoogleUserIdOrNull(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(GoogleClaimTypes.GoogleUserId);
    }

    public static string GetRequiredGoogleUserId(this ClaimsPrincipal principal)
    {
        return principal.GetGoogleUserIdOrNull()
            ?? throw new InvalidOperationException("Authenticated principal does not contain GoogleUserId claim.");
    }

    public static string? GetEmailOrNull(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email);
    }

    public static string? GetNameOrNull(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name);
    }
}