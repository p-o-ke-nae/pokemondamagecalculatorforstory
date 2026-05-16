namespace PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

public class PersistedUserAuthorizationInfo
{
    public string GoogleUserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public ICollection<PersistedUserPermission> Permissions { get; set; } = new List<PersistedUserPermission>();
}