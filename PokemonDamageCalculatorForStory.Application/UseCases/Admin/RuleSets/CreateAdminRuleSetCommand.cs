using MediatR;
using PokemonDamageCalculatorForStory.Application.Auditing;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;

public sealed record CreateAdminRuleSetCommand(
    string ActorGoogleUserId,
    string ActorRole,
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary) : IRequest<AdminRuleSetDto>;

public sealed class CreateAdminRuleSetCommandHandler(
    IRuleSetRepository repository,
    IAdminAuditLogger auditLogger) : IRequestHandler<CreateAdminRuleSetCommand, AdminRuleSetDto>
{
    public async Task<AdminRuleSetDto> Handle(CreateAdminRuleSetCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsBySlugAsync(request.Slug, cancellationToken: cancellationToken))
        {
            var conflict = new ConflictException($"RuleSet slug '{request.Slug}' already exists.");
            await auditLogger.LogAsync(CreateAuditEntry(request, "unknown", "Rejected", conflict.Message), cancellationToken);
            throw conflict;
        }

        var ruleSet = RuleSet.Create(request.Slug, request.Generation, request.Title, request.Version, request.Status, request.Summary);
        await repository.AddAsync(ruleSet, cancellationToken);

        var dto = new AdminRuleSetDto(ruleSet.Id, ruleSet.Slug, ruleSet.Generation, ruleSet.Title, ruleSet.Version, ruleSet.Status, ruleSet.Summary, false);
        await auditLogger.LogAsync(CreateAuditEntry(request, ruleSet.Id.ToString(), "Succeeded", null), cancellationToken);
        return dto;
    }

    private static AdminAuditEntry CreateAuditEntry(CreateAdminRuleSetCommand request, string targetId, string result, string? reason)
        => new(
            DateTimeOffset.UtcNow,
            request.ActorGoogleUserId,
            request.ActorRole,
            "Create",
            nameof(RuleSet),
            targetId,
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
