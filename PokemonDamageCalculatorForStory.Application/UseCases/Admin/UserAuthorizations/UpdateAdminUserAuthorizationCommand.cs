using MediatR;
using PokemonDamageCalculatorForStory.Application.Auditing;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;

public sealed record UpdateAdminUserAuthorizationCommand(
    string ActorGoogleUserId,
    string ActorRole,
    string GoogleUserId,
    string Role,
    IReadOnlyList<string> Permissions) : IRequest<AdminUserAuthorizationDto?>;

public sealed class UpdateAdminUserAuthorizationCommandHandler(
    IUserAuthorizationInfoRepository repository,
    IAdminAuditLogger auditLogger) : IRequestHandler<UpdateAdminUserAuthorizationCommand, AdminUserAuthorizationDto?>
{
    public async Task<AdminUserAuthorizationDto?> Handle(UpdateAdminUserAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByGoogleUserIdAsync(request.GoogleUserId, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        if (string.Equals(existing.Role, AppRoles.Administrator, StringComparison.Ordinal)
            && !string.Equals(request.Role, AppRoles.Administrator, StringComparison.Ordinal)
            && await repository.CountByRoleAsync(AppRoles.Administrator, cancellationToken) == 1)
        {
            var conflict = new ConflictException("The last administrator cannot be changed to a different role.");
            await auditLogger.LogAsync(CreateAuditEntry(request, "Rejected", conflict.Message), cancellationToken);
            throw conflict;
        }

        existing.Update(request.Role, request.Permissions);
        await repository.UpdateAsync(existing, cancellationToken);

        var dto = new AdminUserAuthorizationDto(
            existing.GoogleUserId,
            existing.Role,
            existing.Permissions.OrderBy(permission => permission, StringComparer.Ordinal).ToList().AsReadOnly(),
            string.Equals(existing.Role, AppRoles.Administrator, StringComparison.Ordinal)
                && await repository.CountByRoleAsync(AppRoles.Administrator, cancellationToken) == 1);

        await auditLogger.LogAsync(CreateAuditEntry(request, "Succeeded", null), cancellationToken);
        return dto;
    }

    private static AdminAuditEntry CreateAuditEntry(UpdateAdminUserAuthorizationCommand request, string result, string? reason)
        => new(
            DateTimeOffset.UtcNow,
            request.ActorGoogleUserId,
            request.ActorRole,
            "Update",
            nameof(UserAuthorizationInfo),
            request.GoogleUserId,
            new Dictionary<string, object?>
            {
                ["role"] = request.Role,
                ["permissions"] = request.Permissions.ToArray(),
                ["reason"] = reason
            },
            result);
}
