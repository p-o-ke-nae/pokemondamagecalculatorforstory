using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

public interface IUserAuthorizationInfoRepository
{
    Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default);
}