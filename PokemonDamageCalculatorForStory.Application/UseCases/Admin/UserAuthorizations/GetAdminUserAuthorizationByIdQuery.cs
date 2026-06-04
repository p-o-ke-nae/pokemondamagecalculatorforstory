using MediatR;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;

public sealed record GetAdminUserAuthorizationByIdQuery(string GoogleUserId) : IRequest<AdminUserAuthorizationDto?>;

public sealed class GetAdminUserAuthorizationByIdQueryHandler(IUserAuthorizationInfoRepository repository) : IRequestHandler<GetAdminUserAuthorizationByIdQuery, AdminUserAuthorizationDto?>
{
    public async Task<AdminUserAuthorizationDto?> Handle(GetAdminUserAuthorizationByIdQuery request, CancellationToken cancellationToken)
    {
        var userAuthorization = await repository.FindByGoogleUserIdAsync(request.GoogleUserId, cancellationToken);

        if (userAuthorization is null)
        {
            return null;
        }

        var adminCount = await repository.CountByRoleAsync(AppRoles.Administrator, cancellationToken);

        return new AdminUserAuthorizationDto(
            userAuthorization.GoogleUserId,
            userAuthorization.Role,
            userAuthorization.Permissions.OrderBy(permission => permission, StringComparer.Ordinal).ToList().AsReadOnly(),
            adminCount == 1 && string.Equals(userAuthorization.Role, AppRoles.Administrator, StringComparison.Ordinal));
    }
}
