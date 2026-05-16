namespace PokemonDamageCalculatorForStory.Domain.Entities;

public class UserAuthorizationInfo
{
    private readonly HashSet<string> _permissions;

    private UserAuthorizationInfo(string googleUserId, string role, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            throw new InvalidOperationException("Google user id is required.");
        }

        GoogleUserId = googleUserId;
        Role = string.IsNullOrWhiteSpace(role) ? "Member" : role;
        _permissions = new HashSet<string>(
            permissions.Where(permission => !string.IsNullOrWhiteSpace(permission)),
            StringComparer.OrdinalIgnoreCase);
    }

    public string GoogleUserId { get; }

    public string Role { get; }

    public IReadOnlyCollection<string> Permissions => _permissions;

    public static UserAuthorizationInfo Restore(string googleUserId, string role, IEnumerable<string> permissions)
    {
        return new UserAuthorizationInfo(googleUserId, role, permissions);
    }

    public bool HasRole(string role)
    {
        return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
    }

    public bool HasPermission(string permission)
    {
        return _permissions.Contains(permission);
    }
}