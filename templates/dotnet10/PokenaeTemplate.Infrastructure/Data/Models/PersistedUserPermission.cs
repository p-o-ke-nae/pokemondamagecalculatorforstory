namespace PokenaeTemplate.Infrastructure.Data.Models;

public class PersistedUserPermission
{
    public int Id { get; set; }
    public string GoogleUserId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public PersistedUserAuthorizationInfo? UserAuthorizationInfo { get; set; }
}