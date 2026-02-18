namespace CurrencyConverter.API.Configuration;

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int RequestsPerMinute { get; set; } = 120;
}
