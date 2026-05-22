using MediatR;
using PokemonDamageCalculatorForStory.Application.Auditing;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;

public sealed record DeleteAdminRuleSetCommand(string ActorGoogleUserId, string ActorRole, Guid Id) : IRequest<bool>;

public sealed class DeleteAdminRuleSetCommandHandler(
    IRuleSetRepository repository,
    IAdminAuditLogger auditLogger) : IRequestHandler<DeleteAdminRuleSetCommand, bool>
{
    public async Task<bool> Handle(DeleteAdminRuleSetCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByIdAsync(request.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        if (await repository.IsReferencedByRunsAsync(request.Id, cancellationToken))
        {
            var conflict = new ConflictException("RuleSet is referenced by existing runs and cannot be deleted.");
            await auditLogger.LogAsync(CreateAuditEntry(request, "Rejected", conflict.Message), cancellationToken);
            throw conflict;
        }

        await repository.DeleteAsync(request.Id, cancellationToken);
        await auditLogger.LogAsync(CreateAuditEntry(request, "Succeeded", null), cancellationToken);
        return true;
    }

    private static AdminAuditEntry CreateAuditEntry(DeleteAdminRuleSetCommand request, string result, string? reason)
        => new(
            DateTimeOffset.UtcNow,
            request.ActorGoogleUserId,
            request.ActorRole,
            "Delete",
            nameof(RuleSet),
            request.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["reason"] = reason
            },
            result);
}
