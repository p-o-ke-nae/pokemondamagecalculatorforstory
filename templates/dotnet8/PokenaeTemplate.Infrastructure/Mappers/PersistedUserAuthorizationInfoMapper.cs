using PokenaeTemplate.Domain.Entities;
using PokenaeTemplate.Infrastructure.Data.Models;

namespace PokenaeTemplate.Infrastructure.Mappers;

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