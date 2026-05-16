using PokenaeTemplate.Domain.Entities;
using PokenaeTemplate.Infrastructure.Data.Models;

namespace PokenaeTemplate.Infrastructure.Mappers;

/// <summary>
/// Domain Entity ↔ DB Model マッピング拡張メソッド
/// </summary>
public static class PersistedWeatherForecastMappers
{
    /// <summary>
    /// DB Model を Domain Entity に変換
    /// </summary>
    public static WeatherForecast ToDomainEntity(this PersistedWeatherForecast persisted)
    {
        return WeatherForecast.Restore(
            persisted.Id,
            persisted.Date,
            persisted.TemperatureC,
            persisted.Summary,
            persisted.OwnerGoogleUserId,
            persisted.IsPublic
        );
    }

    /// <summary>
    /// Domain Entity を DB Model に変換
    /// </summary>
    public static PersistedWeatherForecast ToPersistedModel(this WeatherForecast entity)
    {
        return new PersistedWeatherForecast
        {
            Id = entity.Id,
            Date = entity.Date,
            TemperatureC = entity.TemperatureC,
            Summary = entity.Summary,
            OwnerGoogleUserId = entity.OwnerGoogleUserId,
            IsPublic = entity.IsPublic
        };
    }
}
