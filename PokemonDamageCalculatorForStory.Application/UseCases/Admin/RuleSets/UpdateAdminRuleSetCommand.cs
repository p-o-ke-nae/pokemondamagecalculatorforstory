using MediatR;
using PokemonDamageCalculatorForStory.Application.Auditing;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;

public sealed record UpdateAdminRuleSetCommand(
    string ActorGoogleUserId,
    string ActorRole,
    Guid Id,
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary) : IRequest<AdminRuleSetDto?>;

public sealed class UpdateAdminRuleSetCommandHandler(
    IRuleSetRepository repository,
    IAdminAuditLogger auditLogger) : IRequestHandler<UpdateAdminRuleSetCommand, AdminRuleSetDto?>
{
    public async Task<AdminRuleSetDto?> Handle(UpdateAdminRuleSetCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByIdAsync(request.Id, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        if (await repository.ExistsBySlugAsync(request.Slug, request.Id, cancellationToken))
        {
            var conflict = new ConflictException($"RuleSet slug '{request.Slug}' already exists.");
            await auditLogger.LogAsync(CreateAuditEntry(request, "Rejected", conflict.Message), cancellationToken);
            throw conflict;
        }

        existing.Update(request.Slug, request.Generation, request.Title, request.Version, request.Status, request.Summary);
        await repository.UpdateAsync(existing, cancellationToken);

        var dto = new AdminRuleSetDto(
            existing.Id,
            existing.Slug,
            existing.Generation,
            existing.Title,
            existing.Version,
            existing.Status,
            existing.Summary,
            await repository.IsReferencedByRunsAsync(existing.Id, cancellationToken));

        await auditLogger.LogAsync(CreateAuditEntry(request, "Succeeded", null), cancellationToken);
        return dto;
    }

    private static AdminAuditEntry CreateAuditEntry(UpdateAdminRuleSetCommand request, string result, string? reason)
        => new(
            DateTimeOffset.UtcNow,
            request.ActorGoogleUserId,
            request.ActorRole,
            "Update",
            nameof(RuleSet),
            request.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["slug"] = request.Slug,
                ["generation"] = request.Generation,
                ["title"] = request.Title,
                ["version"] = request.Version,
                ["status"] = request.Status,
                ["summary"] = request.Summary,
                ["reason"] = reason
            },
            result);
}
