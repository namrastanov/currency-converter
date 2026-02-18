using System.Text.Json;
using CurrencyConverter.API.Middleware;
using CurrencyConverter.API.Models;
using CurrencyConverter.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace CurrencyConverter.UnitTests.Middleware;

public class GlobalExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _logger = new();
    private readonly Mock<IHostEnvironment> _environment = new();

    public GlobalExceptionHandlingMiddlewareTests()
    {
        _environment.Setup(e => e.EnvironmentName).Returns("Development");
    }

    private GlobalExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new GlobalExceptionHandlingMiddleware(next, _logger.Object, _environment.Object);
    }

    [Fact]
    public async Task Should_Return400_ForCurrencyNotSupportedException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-id";

        var middleware = CreateMiddleware(_ => throw new CurrencyNotSupportedException("TRY"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ErrorResponse>(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        response!.Title.Should().Be("Currency Not Supported");
    }

    [Fact]
    public async Task Should_Return400_ForValidationException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-id";

        var failures = new List<ValidationFailure>
        {
            new("BaseCurrency", "Must be 3 characters")
        };

        var middleware = CreateMiddleware(_ => throw new ValidationException(failures));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Should_Return502_ForExternalApiException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-id";

        var middleware = CreateMiddleware(_ =>
            throw new ExternalApiException("Frankfurter", System.Net.HttpStatusCode.InternalServerError, "Server error"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task Should_Return500_ForUnhandledException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-id";

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Something went wrong"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }
}
