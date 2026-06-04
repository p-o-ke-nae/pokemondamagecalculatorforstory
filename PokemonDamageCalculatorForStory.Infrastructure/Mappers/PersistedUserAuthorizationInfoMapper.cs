using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Mappers;

public static class PersistedUserAuthorizationInfoMapper
{
    public static UserAuthorizationInfo ToDomainEntity(this PersistedUserAuthorizationInfo persisted)
    {
        return UserAuthorizationInfo.Restore(
            persisted.GoogleUserId,
            persisted.Role,
            persisted.Permissions.Select(permission => permission.Permission));
    }

    public static PersistedUserAuthorizationInfo ToPersistedModel(this UserAuthorizationInfo entity)
    {
        return new PersistedUserAuthorizationInfo
        {
            GoogleUserId = entity.GoogleUserId,
            Role = entity.Role,
            Permissions = entity.Permissions
                .Select(permission => new PersistedUserPermission
                {
                    GoogleUserId = entity.GoogleUserId,
                    Permission = permission
                })
                .ToList()
        };
    }
}
