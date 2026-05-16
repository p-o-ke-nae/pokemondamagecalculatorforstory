using PokenaeTemplate.Domain.Entities;

namespace PokenaeTemplate.Domain.Ports;

public interface IUserAuthorizationInfoRepository
{
    Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default);
}