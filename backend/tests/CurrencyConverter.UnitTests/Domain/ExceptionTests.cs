using System.Net;
using CurrencyConverter.Domain.Exceptions;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain;

public class ExceptionTests
{
    [Fact]
    public void CurrencyNotSupportedException_ShouldSetCurrencyCode()
    {
        var ex = new CurrencyNotSupportedException("TRY");

        ex.CurrencyCode.Should().Be("TRY");
        ex.Message.Should().Contain("TRY");
    }

    [Fact]
    public void CurrencyNotSupportedException_ShouldAcceptCustomMessage()
    {
        var ex = new CurrencyNotSupportedException("TRY", "Custom message");

        ex.CurrencyCode.Should().Be("TRY");
        ex.Message.Should().Be("Custom message");
    }

    [Fact]
    public void ExternalApiException_ShouldSetProperties()
    {
        var ex = new ExternalApiException("Frankfurter", HttpStatusCode.InternalServerError, "Server error");

        ex.ProviderName.Should().Be("Frankfurter");
        ex.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Message.Should().Be("Server error");
    }

    [Fact]
    public void ExternalApiException_ShouldAcceptInnerException()
    {
        var inner = new HttpRequestException("Connection refused");
        var ex = new ExternalApiException("Frankfurter", HttpStatusCode.ServiceUnavailable, "API unavailable", inner);

        ex.ProviderName.Should().Be("Frankfurter");
        ex.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        ex.Message.Should().Be("API unavailable");
        ex.InnerException.Should().Be(inner);
    }
}
