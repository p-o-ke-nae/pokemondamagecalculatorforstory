namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 戦場に共有される環境状態を表す。
/// </summary>
public class Field
{
    /// <summary>
    /// フィールド状態を初期化する。
    /// </summary>
    /// <param name="weather">天候。</param>
    /// <param name="terrain">フィールド状態。</param>
    /// <param name="screenState">壁などの補助状態。</param>
    public Field(Weather weather, Terrain terrain, ScreenState screenState)
    {
        Weather = weather;
        Terrain = terrain;
        ScreenState = screenState;
    }

    /// <summary>天候。</summary>
    public Weather Weather { get; }

    /// <summary>フィールド状態。</summary>
    public Terrain Terrain { get; }

    /// <summary>壁などの補助状態。</summary>
    public ScreenState ScreenState { get; }
}