namespace CurrencyConverter.Domain.Exceptions;

public class CurrencyNotSupportedException : Exception
{
    public string CurrencyCode { get; }

    public CurrencyNotSupportedException(string currencyCode)
        : base($"Currency '{currencyCode}' is not supported or is restricted.")
    {
        CurrencyCode = currencyCode;
    }

    public CurrencyNotSupportedException(string currencyCode, string message)
        : base(message)
    {
        CurrencyCode = currencyCode;
    }
}
