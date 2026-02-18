using Asp.Versioning;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers;

/// <summary>
/// Provides latest and historical exchange rate data.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/rates")]
[Authorize]
[Produces("application/json")]
public class RatesController : ControllerBase
{
    private readonly GetLatestRatesUseCase _getLatestRatesUseCase;
    private readonly GetHistoricalRatesUseCase _getHistoricalRatesUseCase;
    private readonly IValidator<GetLatestRatesQuery> _latestRatesValidator;
    private readonly IValidator<GetHistoricalRatesQuery> _historicalRatesValidator;

    public RatesController(
        GetLatestRatesUseCase getLatestRatesUseCase,
        GetHistoricalRatesUseCase getHistoricalRatesUseCase,
        IValidator<GetLatestRatesQuery> latestRatesValidator,
        IValidator<GetHistoricalRatesQuery> historicalRatesValidator)
    {
        _getLatestRatesUseCase = getLatestRatesUseCase;
        _getHistoricalRatesUseCase = getHistoricalRatesUseCase;
        _latestRatesValidator = latestRatesValidator;
        _historicalRatesValidator = historicalRatesValidator;
    }

    /// <summary>
    /// Gets the latest exchange rates for a given base currency.
    /// </summary>
    /// <param name="base">The base currency code (e.g. "USD", "EUR").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest exchange rates relative to the base currency.</returns>
    /// <response code="200">Returns the latest exchange rates.</response>
    /// <response code="400">Validation error – invalid or restricted base currency.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="429">Too many requests – rate limit exceeded.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(ApiResponse<LatestRatesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLatestRates(
        [FromQuery] string @base,
        CancellationToken cancellationToken)
    {
        var query = new GetLatestRatesQuery(@base);
        var validationResult = await _latestRatesValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _getLatestRatesUseCase.ExecuteAsync(query, cancellationToken);
        return Ok(ApiResponse<LatestRatesDto>.Success(result));
    }

    /// <summary>
    /// Gets historical exchange rates for a given base currency and date range.
    /// </summary>
    /// <param name="base">The base currency code (e.g. "USD", "EUR").</param>
    /// <param name="from">Start date of the range (inclusive).</param>
    /// <param name="to">End date of the range (inclusive).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Number of results per page (default: 10, max: 100).</param>
    /// <param name="timezoneOffset">Client timezone offset in minutes (from JS getTimezoneOffset). E.g. UTC+3 = -180.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated historical exchange rates.</returns>
    /// <response code="200">Returns the paginated historical rates with metadata.</response>
    /// <response code="400">Validation error – invalid parameters or restricted currency.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="429">Too many requests – rate limit exceeded.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("historical")]
    [ProducesResponseType(typeof(ApiResponse<HistoricalRatesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHistoricalRates(
        [FromQuery] string @base,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int timezoneOffset = 0,
        CancellationToken cancellationToken = default)
    {
        var query = new GetHistoricalRatesQuery(@base, from, to, page, pageSize, timezoneOffset);
        var validationResult = await _historicalRatesValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _getHistoricalRatesUseCase.ExecuteAsync(query, cancellationToken);

        var metadata = new Dictionary<string, object>
        {
            ["totalCount"] = result.TotalCount,
            ["totalPages"] = result.TotalPages,
            ["page"] = result.Page,
            ["pageSize"] = result.PageSize,
            ["hasNextPage"] = result.HasNextPage,
            ["hasPreviousPage"] = result.HasPreviousPage
        };

        return Ok(ApiResponse<HistoricalRatesDto>.Success(result, metadata));
    }
}
