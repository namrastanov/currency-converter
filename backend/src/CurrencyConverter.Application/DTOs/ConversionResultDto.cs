namespace CurrencyConverter.Application.DTOs;

public record ConversionResultDto(
    string From,
    string To,
    decimal Amount,
    decimal Result,
    decimal Rate,
    DateTime Date);
