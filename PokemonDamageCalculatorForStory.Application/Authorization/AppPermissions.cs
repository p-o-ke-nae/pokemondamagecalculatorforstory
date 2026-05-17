namespace PokemonDamageCalculatorForStory.Application.Authorization;

/// <summary>アプリケーション固有の権限定義です。</summary>
public static class AppPermissions
{
    /// <summary>すべての run を閲覧できる権限です。</summary>
    public const string RunReadAny = "run.read.any";

    /// <summary>master import を実行できる権限です。</summary>
    public const string AdminImportManage = "admin.import.manage";
}
