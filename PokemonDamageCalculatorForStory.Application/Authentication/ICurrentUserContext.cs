namespace PokemonDamageCalculatorForStory.Application.Authentication;

public interface ICurrentUserContext
{
    string GoogleUserId { get; }
    string Role { get; }
}
