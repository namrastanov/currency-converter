using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Rules;

namespace CurrencyConverter.Application.UseCases;

public class ConvertCurrencyUseCase
{
    private readonly ICurrencyProviderFactory _providerFactory;

    public ConvertCurrencyUseCase(ICurrencyProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task<ConversionResultDto> ExecuteAsync(ConvertCurrencyQuery query, CancellationToken cancellationToken = default)
    {
        if (CurrencyRestrictions.IsRestricted(query.From))
            throw new CurrencyNotSupportedException(query.From);

        if (CurrencyRestrictions.IsRestricted(query.To))
            throw new CurrencyNotSupportedException(query.To);

        var provider = _providerFactory.GetProvider();
        var result = await provider.ConvertAsync(query.From, query.To, query.Amount, cancellationToken);

        return new ConversionResultDto(
            result.From,
            result.To,
            result.Amount,
            result.Result,
            result.Rate,
            result.Date);
    }
}
