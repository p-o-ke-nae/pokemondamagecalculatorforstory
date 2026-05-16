using PokenaeTemplate.Domain.Entities;

namespace PokenaeTemplate.Application.Authorization;

public interface IWeatherForecastAccessEvaluator
{
    bool CanRead(WeatherForecast weatherForecast, string? googleUserId, UserAuthorizationInfo? userAuthorizationInfo);

    bool CanManage(WeatherForecast weatherForecast, string? googleUserId, UserAuthorizationInfo? userAuthorizationInfo);
}