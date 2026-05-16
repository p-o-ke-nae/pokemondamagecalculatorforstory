using PokenaeTemplate.Domain.Entities;

namespace PokenaeTemplate.Domain.Ports;

/// <summary>
/// GoogleUserId に紐づくアプリケーション認可情報の取得ポート
/// </summary>
public interface IUserAuthorizationInfoRepository
{
    Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default);
}