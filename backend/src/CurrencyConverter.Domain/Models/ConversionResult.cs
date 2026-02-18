namespace CurrencyConverter.Domain.Models;

public record ConversionResult(
    string From,
    string To,
    decimal Amount,
    decimal Result,
    decimal Rate,
    DateTime Date);
