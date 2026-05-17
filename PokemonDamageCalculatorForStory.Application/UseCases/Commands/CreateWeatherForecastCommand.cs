using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

public class CreateWeatherForecastCommand : IRequest<CreateWeatherForecastResponse>
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public string OwnerGoogleUserId { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

public class CreateWeatherForecastResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public WeatherForecastResponseDto? Data { get; set; }
}
