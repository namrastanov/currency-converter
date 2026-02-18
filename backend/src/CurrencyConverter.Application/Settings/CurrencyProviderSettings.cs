namespace CurrencyConverter.Application.Settings;

public class CurrencyProviderSettings
{
    public const string SectionName = "CurrencyProvider";

    public string DefaultProvider { get; set; } = "Frankfurter";
}
