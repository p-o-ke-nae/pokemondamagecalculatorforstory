using System.Security.Claims;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Authentication;

namespace PokemonDamageCalculatorForStory.Extensions;

/// <summary>ClaimsPrincipal からアプリケーション向け情報を取り出す拡張です。</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>provider 非依存の subject id を取得します。</summary>
    public static string? GetSubjectIdOrNull(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(GoogleClaimTypes.GoogleUserId);
    }

    /// <summary>必須の subject id を取得します。</summary>
    public static string GetRequiredSubjectId(this ClaimsPrincipal principal)
    {
        return principal.GetSubjectIdOrNull()
            ?? throw new InvalidOperationException("Authenticated principal does not contain subject identifier claim.");
    }

    /// <summary>メールアドレスを取得します。</summary>
    public static string? GetEmailOrNull(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email);
    }

    /// <summary>表示名を取得します。</summary>
    public static string? GetNameOrNull(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name);
    }

    /// <summary>アプリケーションロール一覧を取得します。</summary>
    public static IReadOnlyCollection<string> GetAppRoles(this ClaimsPrincipal principal)
    {
        var roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return roles.Length == 0 ? new[] { AppRoles.Member } : roles;
    }
}
