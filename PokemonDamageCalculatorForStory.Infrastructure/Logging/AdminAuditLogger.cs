using Microsoft.Extensions.Logging;
using PokemonDamageCalculatorForStory.Application.Auditing;

namespace PokemonDamageCalculatorForStory.Infrastructure.Logging;

public sealed class AdminAuditLogger(ILogger<AdminAuditLogger> logger) : IAdminAuditLogger
{
    public Task LogAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("AdminAudit {@AdminAuditEntry}", entry);
        return Task.CompletedTask;
    }
}
