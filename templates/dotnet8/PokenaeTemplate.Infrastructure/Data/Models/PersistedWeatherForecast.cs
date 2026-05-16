namespace PokenaeTemplate.Infrastructure.Data.Models;

/// <summary>
/// 永続化される天気予報モデル
/// データベーステーブルに直接マッピング
/// </summary>
public class PersistedWeatherForecast
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public string OwnerGoogleUserId { get; set; } = string.Empty;
    public bool IsPublic { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
