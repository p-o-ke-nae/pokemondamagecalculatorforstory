using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

public interface IUserAuthorizationInfoRepository
{
    Task<IReadOnlyList<UserAuthorizationInfo>> FindAllAsync(CancellationToken cancellationToken = default);
    Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default);
    Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task AddAsync(UserAuthorizationInfo userAuthorizationInfo, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserAuthorizationInfo userAuthorizationInfo, CancellationToken cancellationToken = default);
    Task DeleteAsync(string googleUserId, CancellationToken cancellationToken = default);
}
