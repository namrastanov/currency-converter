using CurrencyConverter.Domain.Models;

namespace CurrencyConverter.Application.DTOs;

public record HistoricalRatesDto(
    string BaseCurrency,
    IReadOnlyList<ExchangeRate> Rates,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);
