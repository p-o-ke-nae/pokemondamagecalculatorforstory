using MediatR;
using PokenaeTemplate.Application.DTOs;

namespace PokenaeTemplate.Application.UseCases.Commands;

public class UpdateWeatherForecastCommand : IRequest<UpdateWeatherForecastResponse>
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public bool IsPublic { get; set; }
}

public class UpdateWeatherForecastResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public WeatherForecastResponseDto? Data { get; set; }
}