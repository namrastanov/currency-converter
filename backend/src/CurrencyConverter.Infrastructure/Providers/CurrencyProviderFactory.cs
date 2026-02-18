using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Infrastructure.Providers;

public class CurrencyProviderFactory : ICurrencyProviderFactory
{
    private readonly Dictionary<string, ICurrencyProvider> _providers;
    private readonly string _defaultProviderName;

    public CurrencyProviderFactory(
        IEnumerable<ICurrencyProvider> providers,
        IOptions<CurrencyProviderSettings> settings)
    {
        _providers = providers.ToDictionary(
            p => p.ProviderName,
            p => p,
            StringComparer.OrdinalIgnoreCase);
        _defaultProviderName = settings.Value.DefaultProvider;
    }

    public ICurrencyProvider GetProvider(string? providerName = null)
    {
        var name = providerName ?? _defaultProviderName;

        if (_providers.TryGetValue(name, out var provider))
            return provider;

        throw new ArgumentException($"Currency provider '{name}' is not registered.");
    }
}
