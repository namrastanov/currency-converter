namespace CurrencyConverter.Application.DTOs;

public record GetCurrenciesQuery;

public record GetLatestRatesQuery(string BaseCurrency);

public record ConvertCurrencyQuery(string From, string To, decimal Amount);

public record GetHistoricalRatesQuery(
    string BaseCurrency,
    DateTime StartDate,
    DateTime EndDate,
    int Page = 1,
    int PageSize = 10,
    int TimezoneOffset = 0);
