namespace PokemonDamageCalculatorForStory.Application.Authorization;

public static class AppPermissions
{
    public const string ManageRuns = "runs.manage.any";
    public const string MastersView = "masters.view";
    public const string ManageRuleSets = "masters.rulesets.manage";
    public const string ManageUserAuthorizations = "masters.user-authorizations.manage";

    public static readonly IReadOnlySet<string> AllowedValues = new HashSet<string>(StringComparer.Ordinal)
    {
        MastersView,
        ManageRuleSets,
        ManageUserAuthorizations
    };
}
