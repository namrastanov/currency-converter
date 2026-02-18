using System.Collections.Frozen;

namespace CurrencyConverter.Domain.Rules;

public static class CurrencyRestrictions
{
    private static readonly FrozenSet<string> ExcludedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "TRY", "PLN", "THB", "MXN"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsRestricted(string currencyCode) =>
        ExcludedCurrencies.Contains(currencyCode);

    public static IReadOnlySet<string> GetExcludedCurrencies() => ExcludedCurrencies;
}
