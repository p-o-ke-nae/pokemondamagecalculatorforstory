namespace PokemonDamageCalculatorForStory.Application.DTOs;

public class UpdateWeatherForecastRequest
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public bool IsPublic { get; set; }
}