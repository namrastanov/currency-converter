namespace CurrencyConverter.API.Configuration;

public class CorsSettings
{
    public const string SectionName = "CorsSettings";

    public string[] AllowedOrigins { get; set; } = { "http://localhost:5173" };
}
