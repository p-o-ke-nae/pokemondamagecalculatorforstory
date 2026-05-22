using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

public class UserAuthorizationInfo
{
    private readonly HashSet<string> _permissions;

    private UserAuthorizationInfo(string googleUserId, string role, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            throw new ValidationException("Google user id is required.");
        }

        GoogleUserId = googleUserId.Trim();
        _permissions = new HashSet<string>(StringComparer.Ordinal);
        Update(role, permissions);
    }

    public string GoogleUserId { get; }

    public string Role { get; private set; } = string.Empty;

    public IReadOnlyCollection<string> Permissions => _permissions;

    public static UserAuthorizationInfo Restore(string googleUserId, string role, IEnumerable<string> permissions)
    {
        return new UserAuthorizationInfo(googleUserId, role, permissions);
    }

    public static UserAuthorizationInfo Create(string googleUserId, string role, IEnumerable<string> permissions)
    {
        return new UserAuthorizationInfo(googleUserId, role, permissions);
    }

    public void Update(string role, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ValidationException("Role is required.");
        }

        var normalizedPermissions = permissions?.Select(permission => permission?.Trim() ?? string.Empty).ToList()
            ?? [];

        if (normalizedPermissions.Any(string.IsNullOrWhiteSpace))
        {
            throw new ValidationException("Permission must not be blank.");
        }

        if (normalizedPermissions.Count != normalizedPermissions.Distinct(StringComparer.Ordinal).Count())
        {
            throw new ValidationException("Permission must be unique.");
        }

        Role = role.Trim();
        _permissions.Clear();

        foreach (var permission in normalizedPermissions)
        {
            _permissions.Add(permission);
        }
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
