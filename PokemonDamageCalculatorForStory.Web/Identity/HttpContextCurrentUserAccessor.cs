using PokemonDamageCalculatorForStory.Application.Identity;
using PokemonDamageCalculatorForStory.Extensions;

namespace PokemonDamageCalculatorForStory.Web.Identity;

/// <summary>HTTP コンテキストから現在ユーザーを解決します。</summary>
public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>アクセサーを初期化します。</summary>
    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public CurrentUser? GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = principal.GetSubjectIdOrNull();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return new CurrentUser(
            userId,
            principal.GetNameOrNull() ?? userId,
            principal.GetEmailOrNull(),
            principal.GetAppRoles());
    }
}
