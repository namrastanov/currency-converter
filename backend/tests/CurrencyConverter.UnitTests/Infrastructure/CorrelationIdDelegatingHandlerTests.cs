using CurrencyConverter.Infrastructure.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class CorrelationIdDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_ShouldAddCorrelationIdHeader_WhenPresentInContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "test-correlation-id";

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var handler = new CorrelationIdDelegatingHandler(accessor.Object)
        {
            InnerHandler = new TestHandler()
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        await client.GetAsync("/test");

        TestHandler.LastRequest!.Headers.GetValues("X-Correlation-ID")
            .Should().Contain("test-correlation-id");
    }

    [Fact]
    public async Task SendAsync_ShouldNotAddHeader_WhenNoCorrelationIdInContext()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var handler = new CorrelationIdDelegatingHandler(accessor.Object)
        {
            InnerHandler = new TestHandler()
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        await client.GetAsync("/test");

        TestHandler.LastRequest!.Headers.Contains("X-Correlation-ID").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ShouldNotAddHeader_WhenHttpContextIsNull()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var handler = new CorrelationIdDelegatingHandler(accessor.Object)
        {
            InnerHandler = new TestHandler()
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        await client.GetAsync("/test");

        TestHandler.LastRequest!.Headers.Contains("X-Correlation-ID").Should().BeFalse();
    }

    private class TestHandler : HttpMessageHandler
    {
        public static HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
