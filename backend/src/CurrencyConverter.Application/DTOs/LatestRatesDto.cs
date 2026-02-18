namespace CurrencyConverter.Application.DTOs;

public record LatestRatesDto(
    string BaseCurrency,
    DateTime Date,
    Dictionary<string, decimal> Rates);
