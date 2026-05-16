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
}