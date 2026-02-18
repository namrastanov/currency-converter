using System.Net;

namespace CurrencyConverter.Domain.Exceptions;

public class ExternalApiException : Exception
{
    public string ProviderName { get; }
    public HttpStatusCode StatusCode { get; }

    public ExternalApiException(string providerName, HttpStatusCode statusCode, string message)
        : base(message)
    {
        ProviderName = providerName;
        StatusCode = statusCode;
    }

    public ExternalApiException(string providerName, HttpStatusCode statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName;
        StatusCode = statusCode;
    }
}
