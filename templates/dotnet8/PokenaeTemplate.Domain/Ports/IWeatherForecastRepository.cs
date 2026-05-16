using PokenaeTemplate.Domain.Entities;

namespace PokenaeTemplate.Domain.Ports;

/// <summary>
/// 天気予報リポジトリ のポートインターフェース
/// Infrastructure層で実装される
/// </summary>
public interface IWeatherForecastRepository
{
    /// <summary>
    /// ID で天気予報を取得
    /// </summary>
    Task<WeatherForecast?> FindByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての天気予報を取得
    /// </summary>
    Task<IReadOnlyList<WeatherForecast>> FindAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 天気予報を保存（作成または更新）
    /// </summary>
    Task<WeatherForecast> SaveAsync(WeatherForecast weather, CancellationToken cancellationToken = default);

    /// <summary>
    /// 天気予報を削除
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
