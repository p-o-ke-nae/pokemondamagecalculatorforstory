namespace PokenaeTemplate.Application.DTOs;

/// <summary>
/// 天気予報作成リクエスト DTO
/// API がクライアントから受け取るデータ構造
/// </summary>
public class CreateWeatherForecastRequest
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public bool IsPublic { get; set; }
}
