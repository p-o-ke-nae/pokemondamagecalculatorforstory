using PokenaeTemplate.Application.DTOs;
using PokenaeTemplate.Domain.Entities;

namespace PokenaeTemplate.Application.Mappers;

/// <summary>
/// WeatherForecast Entity → DTO マッピング拡張メソッド
/// </summary>
public static class WeatherForecastMappers
{
    /// <summary>
    /// Domain Entity を Response DTO に変換
    /// </summary>
    public static WeatherForecastResponseDto ToWeatherForecastResponseDto(this WeatherForecast entity)
    {
        return new WeatherForecastResponseDto
        {
            Id = entity.Id,
            Date = entity.Date,
            TemperatureC = entity.TemperatureC,
            TemperatureF = entity.TemperatureF,
            Summary = entity.Summary,
            IsPublic = entity.IsPublic
        };
    }
}
