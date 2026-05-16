namespace PokenaeTemplate.Application.DTOs;

/// <summary>
/// 天気予報レスポンス DTO
/// API がクライアントに返すデータ構造（DB 鏡合わせ）
/// </summary>
public class WeatherForecastResponseDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF { get; set; }
    public string? Summary { get; set; }
    public bool IsPublic { get; set; }
}
