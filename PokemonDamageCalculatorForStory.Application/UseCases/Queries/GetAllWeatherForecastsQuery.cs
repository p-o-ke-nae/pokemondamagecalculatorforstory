using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public class GetAllWeatherForecastsQuery : IRequest<GetAllWeatherForecastsResponse>
{
    public string? RequestingGoogleUserId { get; set; }
}

public class GetAllWeatherForecastsResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<WeatherForecastResponseDto>? Data { get; set; }
}
