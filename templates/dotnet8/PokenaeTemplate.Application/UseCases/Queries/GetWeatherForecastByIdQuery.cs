using MediatR;
using PokenaeTemplate.Application.DTOs;

namespace PokenaeTemplate.Application.UseCases.Queries;

/// <summary>
/// 天気予報ID検索クエリ
/// </summary>
public class GetWeatherForecastByIdQuery : IRequest<GetWeatherForecastByIdResponse>
{
    public int Id { get; set; }
    public string? RequestingGoogleUserId { get; set; }
}

/// <summary>
/// クエリ実行結果
/// </summary>
public class GetWeatherForecastByIdResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public WeatherForecastResponseDto? Data { get; set; }
}
