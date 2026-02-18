using Asp.Versioning;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers;

/// <summary>
/// Converts amounts between currencies.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/convert")]
[Authorize]
[Produces("application/json")]
public class ConversionController : ControllerBase
{
    private readonly ConvertCurrencyUseCase _convertCurrencyUseCase;
    private readonly IValidator<ConvertCurrencyQuery> _validator;

    public ConversionController(
        ConvertCurrencyUseCase convertCurrencyUseCase,
        IValidator<ConvertCurrencyQuery> validator)
    {
        _convertCurrencyUseCase = convertCurrencyUseCase;
        _validator = validator;
    }

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    /// <param name="from">Source currency code (e.g. "USD").</param>
    /// <param name="to">Target currency code (e.g. "EUR").</param>
    /// <param name="amount">Amount to convert (must be greater than 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion result including the rate and converted amount.</returns>
    /// <response code="200">Returns the conversion result.</response>
    /// <response code="400">Validation error – invalid currencies, restricted currency, or invalid amount.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="429">Too many requests – rate limit exceeded.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ConversionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Convert(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken)
    {
        var query = new ConvertCurrencyQuery(from, to, amount);
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _convertCurrencyUseCase.ExecuteAsync(query, cancellationToken);
        return Ok(ApiResponse<ConversionResultDto>.Success(result));
    }
}
