namespace PokemonDamageCalculatorForStory.Domain.Exceptions;

/// <summary>
/// 天気予報が見つからない場合の例外
/// </summary>
public class WeatherForecastNotFoundException : DomainException
{
    public WeatherForecastNotFoundException(int id) 
        : base($"Weather forecast with ID {id} not found.") { }
}

/// <summary>
/// ドメイン例外の基底クラス
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
