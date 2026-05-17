namespace PokemonDamageCalculatorForStory.Authorization;

/// <summary>Web API で利用する認可ポリシー名です。</summary>
public static class AppPolicies
{
    /// <summary>認証済みユーザーなら利用できる所有者向けポリシーです。</summary>
    public const string RunOwner = "RunOwner";

    /// <summary>管理者向けポリシーです。</summary>
    public const string AdminOnly = "AdminOnly";
}
