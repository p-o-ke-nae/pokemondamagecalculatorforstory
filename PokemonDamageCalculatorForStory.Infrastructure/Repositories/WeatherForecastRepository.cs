using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;
using PokemonDamageCalculatorForStory.Infrastructure.Mappers;

namespace PokemonDamageCalculatorForStory.Infrastructure.Repositories;

public class WeatherForecastRepository : IWeatherForecastRepository
{
    private readonly AppDbContext _context;

    public WeatherForecastRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WeatherForecast?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedWeatherForecasts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return persisted?.ToDomainEntity();
    }

    public async Task<IReadOnlyList<WeatherForecast>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedWeatherForecasts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return persisted.Select(x => x.ToDomainEntity()).ToList().AsReadOnly();
    }

    public async Task<WeatherForecast> SaveAsync(WeatherForecast weather, CancellationToken cancellationToken = default)
    {
        var persisted = weather.ToPersistedModel();

        if (persisted.Id == 0)
        {
            _context.PersistedWeatherForecasts.Add(persisted);
        }
        else
        {
            _context.PersistedWeatherForecasts.Update(persisted);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return persisted.ToDomainEntity();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedWeatherForecasts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (persisted == null)
        {
            return false;
        }

        _context.PersistedWeatherForecasts.Remove(persisted);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
