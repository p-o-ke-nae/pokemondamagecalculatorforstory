using PokenaeTemplate.Domain.Entities;

namespace PokenaeTemplate.Application.Authorization;

public class WeatherForecastAccessEvaluator : IWeatherForecastAccessEvaluator
{
    public bool CanRead(WeatherForecast weatherForecast, string? googleUserId, UserAuthorizationInfo? userAuthorizationInfo)
    {
        return weatherForecast.IsPublic
            || IsOwner(weatherForecast, googleUserId)
            || HasReadAllAccess(userAuthorizationInfo);
    }

    public bool CanManage(WeatherForecast weatherForecast, string? googleUserId, UserAuthorizationInfo? userAuthorizationInfo)
    {
        return IsOwner(weatherForecast, googleUserId)
            || HasManageAllAccess(userAuthorizationInfo);
    }

    private static bool IsOwner(WeatherForecast weatherForecast, string? googleUserId)
    {
        return !string.IsNullOrWhiteSpace(googleUserId)
            && string.Equals(weatherForecast.OwnerGoogleUserId, googleUserId, StringComparison.Ordinal);
    }

    private static bool HasReadAllAccess(UserAuthorizationInfo? userAuthorizationInfo)
    {
        return userAuthorizationInfo?.HasRole(AppRoles.Administrator) == true
            || userAuthorizationInfo?.HasPermission(AppPermissions.WeatherForecastReadPrivate) == true
            || userAuthorizationInfo?.HasPermission(AppPermissions.WeatherForecastManageAny) == true;
    }

    private static bool HasManageAllAccess(UserAuthorizationInfo? userAuthorizationInfo)
    {
        return userAuthorizationInfo?.HasRole(AppRoles.Administrator) == true
            || userAuthorizationInfo?.HasPermission(AppPermissions.WeatherForecastManageAny) == true;
    }
}