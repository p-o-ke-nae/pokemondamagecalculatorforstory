using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Mappers;

public static class PersistedWeatherForecastMappers
{
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
