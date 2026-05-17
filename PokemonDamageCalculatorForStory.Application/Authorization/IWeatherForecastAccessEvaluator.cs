using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Application.Authorization;

public interface IWeatherForecastAccessEvaluator
{
    bool CanRead(WeatherForecast weatherForecast, string? googleUserId, UserAuthorizationInfo? userAuthorizationInfo);

    bool CanManage(WeatherForecast weatherForecast, string? googleUserId, UserAuthorizationInfo? userAuthorizationInfo);
}