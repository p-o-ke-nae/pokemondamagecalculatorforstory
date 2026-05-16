namespace PokenaeTemplate.Domain.Entities;

/// <summary>
/// 天気予報ドメインエンティティ
/// ビジネスロジックと不変性を保有
/// </summary>
public class WeatherForecast
{
    public int Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int TemperatureC { get; private set; }
    public string? Summary { get; private set; }
    public string OwnerGoogleUserId { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    private WeatherForecast() { }

    /// <summary>
    /// 新規天気予報エンティティを生成
    /// </summary>
    public static WeatherForecast Create(
        DateOnly date,
        int temperatureC,
        string? summary,
        string ownerGoogleUserId,
        bool isPublic)
    {
        EnsureValidState(temperatureC, ownerGoogleUserId);

        return new WeatherForecast
        {
            Date = date,
            TemperatureC = temperatureC,
            Summary = summary,
            OwnerGoogleUserId = ownerGoogleUserId,
            IsPublic = isPublic
        };
    }

    /// <summary>
    /// DB から復元されたエンティティを生成
    /// </summary>
    public static WeatherForecast Restore(
        int id,
        DateOnly date,
        int temperatureC,
        string? summary,
        string ownerGoogleUserId,
        bool isPublic)
    {
        EnsureValidState(temperatureC, ownerGoogleUserId);

        return new WeatherForecast
        {
            Id = id,
            Date = date,
            TemperatureC = temperatureC,
            Summary = summary,
            OwnerGoogleUserId = ownerGoogleUserId,
            IsPublic = isPublic
        };
    }

    public void Update(DateOnly date, int temperatureC, string? summary, bool isPublic)
    {
        EnsureValidState(temperatureC, OwnerGoogleUserId);

        Date = date;
        TemperatureC = temperatureC;
        Summary = summary;
        IsPublic = isPublic;
    }

    /// <summary>
    /// 気温が有効な範囲かチェック
    /// </summary>
    private static bool IsValidTemperature(int temperatureC) => temperatureC >= -50 && temperatureC <= 60;

    private static void EnsureValidState(int temperatureC, string ownerGoogleUserId)
    {
        if (!IsValidTemperature(temperatureC))
        {
            throw new InvalidOperationException($"Temperature {temperatureC}°C is out of valid range (-50 to 60)");
        }

        if (string.IsNullOrWhiteSpace(ownerGoogleUserId))
        {
            throw new InvalidOperationException("Owner Google user id is required.");
        }
    }
}
