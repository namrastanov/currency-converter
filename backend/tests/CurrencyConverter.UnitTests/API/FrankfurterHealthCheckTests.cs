using System.Net;
using CurrencyConverter.API.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Moq.Protected;

namespace CurrencyConverter.UnitTests.API;

public class FrankfurterHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenApiResponds()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.frankfurter.dev") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrankfurterHealthCheck")).Returns(httpClient);

        var healthCheck = new FrankfurterHealthCheck(factory.Object);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenApiReturnsError()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.frankfurter.dev") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrankfurterHealthCheck")).Returns(httpClient);

        var healthCheck = new FrankfurterHealthCheck(factory.Object);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenExceptionOccurs()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.frankfurter.dev") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrankfurterHealthCheck")).Returns(httpClient);

        var healthCheck = new FrankfurterHealthCheck(factory.Object);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
