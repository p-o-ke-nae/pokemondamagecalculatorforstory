using MediatR;
using PokemonDamageCalculatorForStory.Application.Auditing;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;

public sealed record CreateAdminUserAuthorizationCommand(
    string ActorGoogleUserId,
    string ActorRole,
    string GoogleUserId,
    string Role,
    IReadOnlyList<string> Permissions) : IRequest<AdminUserAuthorizationDto>;

public sealed class CreateAdminUserAuthorizationCommandHandler(
    IUserAuthorizationInfoRepository repository,
    IAdminAuditLogger auditLogger) : IRequestHandler<CreateAdminUserAuthorizationCommand, AdminUserAuthorizationDto>
{
    public async Task<AdminUserAuthorizationDto> Handle(CreateAdminUserAuthorizationCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByGoogleUserIdAsync(request.GoogleUserId, cancellationToken))
        {
            var conflict = new ConflictException($"UserAuthorizationInfo '{request.GoogleUserId}' already exists.");
            await auditLogger.LogAsync(CreateAuditEntry(request, "Rejected", conflict.Message), cancellationToken);
            throw conflict;
        }

        var userAuthorization = UserAuthorizationInfo.Create(request.GoogleUserId, request.Role, request.Permissions);
        await repository.AddAsync(userAuthorization, cancellationToken);

        var dto = new AdminUserAuthorizationDto(
            userAuthorization.GoogleUserId,
            userAuthorization.Role,
            userAuthorization.Permissions.OrderBy(permission => permission, StringComparer.Ordinal).ToList().AsReadOnly(),
            string.Equals(userAuthorization.Role, AppRoles.Administrator, StringComparison.Ordinal)
                && await repository.CountByRoleAsync(AppRoles.Administrator, cancellationToken) == 1);

        await auditLogger.LogAsync(CreateAuditEntry(request, "Succeeded", null), cancellationToken);
        return dto;
    }

    private static AdminAuditEntry CreateAuditEntry(CreateAdminUserAuthorizationCommand request, string result, string? reason)
        => new(
            DateTimeOffset.UtcNow,
            request.ActorGoogleUserId,
            request.ActorRole,
            "Create",
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
