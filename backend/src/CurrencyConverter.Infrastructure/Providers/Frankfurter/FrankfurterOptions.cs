namespace CurrencyConverter.Infrastructure.Providers.Frankfurter;

public class FrankfurterOptions
{
    public const string SectionName = "Frankfurter";

    public string BaseUrl { get; set; } = "https://api.frankfurter.dev";
    public int TimeoutSeconds { get; set; } = 10;
}
