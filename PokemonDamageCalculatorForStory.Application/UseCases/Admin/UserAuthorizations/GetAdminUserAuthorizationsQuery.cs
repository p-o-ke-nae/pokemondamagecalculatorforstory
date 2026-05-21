using MediatR;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;

public sealed record GetAdminUserAuthorizationsQuery : IRequest<IReadOnlyList<AdminUserAuthorizationDto>>;

public sealed class GetAdminUserAuthorizationsQueryHandler(IUserAuthorizationInfoRepository repository) : IRequestHandler<GetAdminUserAuthorizationsQuery, IReadOnlyList<AdminUserAuthorizationDto>>
{
    public async Task<IReadOnlyList<AdminUserAuthorizationDto>> Handle(GetAdminUserAuthorizationsQuery request, CancellationToken cancellationToken)
    {
        var users = await repository.FindAllAsync(cancellationToken);
        var adminCount = await repository.CountByRoleAsync(AppRoles.Administrator, cancellationToken);

        return users
            .OrderBy(user => user.GoogleUserId, StringComparer.Ordinal)
            .Select(user => new AdminUserAuthorizationDto(
                user.GoogleUserId,
                user.Role,
                user.Permissions.OrderBy(permission => permission, StringComparer.Ordinal).ToList().AsReadOnly(),
                adminCount == 1 && string.Equals(user.Role, AppRoles.Administrator, StringComparison.Ordinal)))
            .ToList()
            .AsReadOnly();
    }
}
