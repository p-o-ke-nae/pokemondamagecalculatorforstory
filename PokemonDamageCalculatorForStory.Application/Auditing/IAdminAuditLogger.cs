namespace PokemonDamageCalculatorForStory.Application.Auditing;

public interface IAdminAuditLogger
{
    Task LogAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default);
}
