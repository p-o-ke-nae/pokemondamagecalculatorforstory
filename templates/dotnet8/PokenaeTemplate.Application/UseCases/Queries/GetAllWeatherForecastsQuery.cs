using MediatR;
using PokenaeTemplate.Application.DTOs;

namespace PokenaeTemplate.Application.UseCases.Queries;

/// <summary>
/// 天気予報一覧取得クエリ
/// </summary>
public class GetAllWeatherForecastsQuery : IRequest<GetAllWeatherForecastsResponse>
{
    public string? RequestingGoogleUserId { get; set; }
}

/// <summary>
/// クエリ実行結果
/// </summary>
public class GetAllWeatherForecastsResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<WeatherForecastResponseDto>? Data { get; set; }
}
