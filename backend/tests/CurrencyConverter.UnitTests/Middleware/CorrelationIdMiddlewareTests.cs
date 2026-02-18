using CurrencyConverter.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace CurrencyConverter.UnitTests.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Should_GenerateCorrelationId_WhenNotPresent()
    {
        var context = new DefaultHttpContext();
        string? capturedCorrelationId = null;

        var middleware = new CorrelationIdMiddleware(next: (ctx) =>
        {
            capturedCorrelationId = ctx.Items["CorrelationId"]?.ToString();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        capturedCorrelationId.Should().NotBeNullOrEmpty();
        context.Response.Headers["X-Correlation-ID"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_UseExistingCorrelationId_WhenPresent()
    {
        var context = new DefaultHttpContext();
        var expectedId = "test-correlation-id-123";
        context.Request.Headers["X-Correlation-ID"] = expectedId;

        string? capturedCorrelationId = null;
        var middleware = new CorrelationIdMiddleware(next: (ctx) =>
        {
            capturedCorrelationId = ctx.Items["CorrelationId"]?.ToString();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        capturedCorrelationId.Should().Be(expectedId);
        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be(expectedId);
    }

    [Fact]
    public async Task Should_SetResponseHeader()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(next: _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey("X-Correlation-ID");
    }
}
