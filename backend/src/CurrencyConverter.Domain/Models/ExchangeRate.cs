namespace CurrencyConverter.Domain.Models;

public record ExchangeRate(
    string BaseCurrency,
    DateTime Date,
    Dictionary<string, decimal> Rates);
