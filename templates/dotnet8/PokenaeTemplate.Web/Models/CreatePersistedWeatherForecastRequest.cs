namespace PokenaeTemplate.Models;

public class CreatePersistedWeatherForecastRequest
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
}
