using MediatR;
using PokemonDamageCalculatorForStory.Application.Auditing;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;

public sealed record DeleteAdminUserAuthorizationCommand(string ActorGoogleUserId, string ActorRole, string GoogleUserId) : IRequest<bool>;

public sealed class DeleteAdminUserAuthorizationCommandHandler(
    IUserAuthorizationInfoRepository repository,
    IAdminAuditLogger auditLogger) : IRequestHandler<DeleteAdminUserAuthorizationCommand, bool>
{
    public async Task<bool> Handle(DeleteAdminUserAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByGoogleUserIdAsync(request.GoogleUserId, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        if (string.Equals(existing.Role, AppRoles.Administrator, StringComparison.Ordinal)
            && await repository.CountByRoleAsync(AppRoles.Administrator, cancellationToken) == 1)
        {
            var conflict = new ConflictException("The last administrator cannot be deleted.");
            await auditLogger.LogAsync(CreateAuditEntry(request, "Rejected", conflict.Message), cancellationToken);
            throw conflict;
        }

        await repository.DeleteAsync(request.GoogleUserId, cancellationToken);
        await auditLogger.LogAsync(CreateAuditEntry(request, "Succeeded", null), cancellationToken);
        return true;
    }

    private static AdminAuditEntry CreateAuditEntry(DeleteAdminUserAuthorizationCommand request, string result, string? reason)
        => new(
            DateTimeOffset.UtcNow,
            request.ActorGoogleUserId,
            request.ActorRole,
            "Delete",
            nameof(UserAuthorizationInfo),
            request.GoogleUserId,
            new Dictionary<string, object?>
            {
                ["reason"] = reason
            },
            result);
}
