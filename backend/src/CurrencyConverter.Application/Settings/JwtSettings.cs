namespace CurrencyConverter.Application.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CurrencyConverter";
    public string Audience { get; set; } = "CurrencyConverterClient";
    public int ExpirationMinutes { get; set; } = 60;
}
