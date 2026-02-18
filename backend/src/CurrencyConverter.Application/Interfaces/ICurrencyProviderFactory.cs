using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Application.Interfaces;

public interface ICurrencyProviderFactory
{
    ICurrencyProvider GetProvider(string? providerName = null);
}
