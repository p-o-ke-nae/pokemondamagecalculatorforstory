namespace PokemonDamageCalculatorForStory.Application.Identity;

/// <summary>アプリケーションで扱う provider 非依存の現在ユーザー情報です。</summary>
/// <param name="UserId">アプリケーション内の所有者識別子です。</param>
/// <param name="DisplayName">表示名です。</param>
/// <param name="Email">メールアドレスです。</param>
/// <param name="Roles">保持ロール一覧です。</param>
public sealed record CurrentUser(
    string UserId,
    string DisplayName,
    string? Email,
    IReadOnlyCollection<string> Roles)
{
    /// <summary>管理者ロールを持つかどうかを返します。</summary>
    public bool IsAdministrator => Roles.Contains(Authorization.AppRoles.Administrator, StringComparer.OrdinalIgnoreCase);
}

/// <summary>現在ユーザー情報へのアクセスを抽象化します。</summary>
public interface ICurrentUserAccessor
{
    /// <summary>現在ユーザーを取得します。未認証の場合は <see langword="null" /> を返します。</summary>
    CurrentUser? GetCurrentUser();
}
