using PokenaeTemplate.Application.DTOs;
using PokenaeTemplate.Domain.Entities;

namespace PokenaeTemplate.Application.Mappers;

public static class WeatherForecastMappers
{
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
