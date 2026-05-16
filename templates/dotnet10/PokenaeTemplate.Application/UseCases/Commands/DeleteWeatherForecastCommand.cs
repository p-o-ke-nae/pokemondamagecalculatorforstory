using MediatR;

namespace PokenaeTemplate.Application.UseCases.Commands;

public class DeleteWeatherForecastCommand : IRequest<DeleteWeatherForecastResponse>
{
    public int Id { get; set; }
}

public class DeleteWeatherForecastResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}