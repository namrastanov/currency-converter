using Asp.Versioning;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers;

/// <summary>
/// Provides available currency information.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/currencies")]
[Authorize]
[Produces("application/json")]
public class CurrenciesController : ControllerBase
{
    private readonly GetCurrenciesUseCase _getCurrenciesUseCase;

    public CurrenciesController(GetCurrenciesUseCase getCurrenciesUseCase)
    {
        _getCurrenciesUseCase = getCurrenciesUseCase;
    }

    /// <summary>
    /// Gets all available currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available currencies with their restriction status.</returns>
    /// <response code="200">Returns the list of currencies.</response>
    /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
    /// <response code="429">Too many requests – rate limit exceeded.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CurrencyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrencies(CancellationToken cancellationToken)
    {
        var currencies = await _getCurrenciesUseCase.ExecuteAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CurrencyDto>>.Success(currencies));
    }
}
