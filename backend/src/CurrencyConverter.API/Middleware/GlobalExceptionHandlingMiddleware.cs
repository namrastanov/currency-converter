using System.Net;
using System.Text.Json;
using CurrencyConverter.API.Models;
using CurrencyConverter.Domain.Exceptions;
using FluentValidation;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CurrencyConverter.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            InvalidCredentialsException => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Unauthorized",
                Status = (int)HttpStatusCode.Unauthorized,
                Detail = "Invalid username or password."
            },
            UserAlreadyExistsException ex => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Conflict",
                Status = (int)HttpStatusCode.Conflict,
                Detail = ex.Message
            },
            CurrencyNotSupportedException ex => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Currency Not Supported",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = ex.Message
            },
            ValidationException ex => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Failed",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "One or more validation errors occurred.",
                Errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())
            },
            ExternalApiException ex => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                Title = "External Service Error",
                Status = (int)HttpStatusCode.BadGateway,
                Detail = ex.Message
            },
            BrokenCircuitException => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                Title = "Service Unavailable",
                Status = (int)HttpStatusCode.ServiceUnavailable,
                Detail = "The external currency service is temporarily unavailable. Please try again later."
            },
            TimeoutRejectedException => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                Title = "Service Unavailable",
                Status = (int)HttpStatusCode.ServiceUnavailable,
                Detail = "The request to the external currency service timed out. Please try again later."
            },
            _ => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.Status;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }
}
