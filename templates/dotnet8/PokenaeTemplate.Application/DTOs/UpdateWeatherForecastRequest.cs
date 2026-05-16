namespace PokenaeTemplate.Application.DTOs;

/// <summary>
/// 天気予報更新リクエスト DTO
/// </summary>
public class UpdateWeatherForecastRequest
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public bool IsPublic { get; set; }
}