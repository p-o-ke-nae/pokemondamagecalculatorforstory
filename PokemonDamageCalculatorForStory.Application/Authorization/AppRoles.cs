namespace PokemonDamageCalculatorForStory.Application.Authorization;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string MasterEditor = "MasterEditor";
    public const string Member = "Member";

    public static readonly IReadOnlySet<string> AllowedValues = new HashSet<string>(StringComparer.Ordinal)
    {
        Administrator,
        MasterEditor,
        Member
    };
}
