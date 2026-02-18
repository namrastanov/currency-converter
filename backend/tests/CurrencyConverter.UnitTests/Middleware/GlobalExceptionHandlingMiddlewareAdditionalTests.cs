using System.Text.Json;
using CurrencyConverter.API.Middleware;
using CurrencyConverter.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CurrencyConverter.UnitTests.Middleware;

public class GlobalExceptionHandlingMiddlewareAdditionalTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _logger = new();
    private readonly Mock<IHostEnvironment> _environment = new();

    private GlobalExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new GlobalExceptionHandlingMiddleware(next, _logger.Object, _environment.Object);
    }

    [Fact]
    public async Task Should_Return503_ForBrokenCircuitException()
    {
        _environment.Setup(e => e.EnvironmentName).Returns("Development");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-id";

        var middleware = CreateMiddleware(_ => throw new BrokenCircuitException());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(503);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ErrorResponse>(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        response!.Title.Should().Be("Service Unavailable");
    }

    [Fact]
    public async Task Should_Return503_ForTimeoutRejectedException()
    {
        _environment.Setup(e => e.EnvironmentName).Returns("Development");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-id";

        var middleware = CreateMiddleware(_ => throw new TimeoutRejectedException());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(503);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ErrorResponse>(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        response!.Title.Should().Be("Service Unavailable");
        response.Detail.Should().Contain("timed out");
    }

    [Fact]
    public async Task Should_HideDetailInProduction_ForUnhandledException()
    {
        _environment.Setup(e => e.EnvironmentName).Returns("Production");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-id";

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Sensitive details"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ErrorResponse>(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        response!.Detail.Should().NotContain("Sensitive details");
        response.Detail.Should().Be("An unexpected error occurred. Please try again later.");
    }

    [Fact]
    public async Task Should_PassThrough_WhenNoException()
    {
        _environment.Setup(e => e.EnvironmentName).Returns("Development");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = CreateMiddleware(_ =>
        {
            _.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Should_HandleMissingCorrelationId()
    {
        _environment.Setup(e => e.EnvironmentName).Returns("Development");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Error"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }
}
