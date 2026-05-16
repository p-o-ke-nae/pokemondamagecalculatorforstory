namespace PokemonDamageCalculatorForStory.Authentication;

public interface IGoogleAccessTokenValidationService
{
    Task<GoogleAccessTokenValidationResult> ValidateAsync(string accessToken, CancellationToken cancellationToken = default);
}