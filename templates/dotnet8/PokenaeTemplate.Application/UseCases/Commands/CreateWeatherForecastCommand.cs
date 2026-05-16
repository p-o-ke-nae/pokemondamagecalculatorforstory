using MediatR;
using PokenaeTemplate.Application.DTOs;

namespace PokenaeTemplate.Application.UseCases.Commands;

/// <summary>
/// 天気予報作成コマンド
/// </summary>
public class CreateWeatherForecastCommand : IRequest<CreateWeatherForecastResponse>
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public string OwnerGoogleUserId { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

/// <summary>
/// コマンド実行結果
/// </summary>
public class CreateWeatherForecastResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public WeatherForecastResponseDto? Data { get; set; }
}
