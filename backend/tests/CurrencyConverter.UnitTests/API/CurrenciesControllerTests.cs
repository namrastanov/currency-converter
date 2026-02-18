using CurrencyConverter.API.Controllers;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Application.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace CurrencyConverter.UnitTests.API;

public class CurrenciesControllerTests
{
    private readonly Mock<ICurrencyProviderFactory> _providerFactory = new();
    private readonly Mock<ICacheService> _cacheService = new();

    [Fact]
    public async Task GetCurrencies_ShouldReturnOk_WithCurrencyList()
    {
        var currencies = new List<CurrencyDto>
        {
            new("USD", "US Dollar", false),
            new("EUR", "Euro", false),
            new("TRY", "Turkish Lira", true)
        };

        _cacheService.Setup(c => c.GetAsync<List<CurrencyDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);

        var useCase = new GetCurrenciesUseCase(
            _providerFactory.Object,
            _cacheService.Object,
            Options.Create(new CacheSettings()));
        var controller = new CurrenciesController(useCase);

        var result = await controller.GetCurrencies(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<IReadOnlyList<CurrencyDto>>>().Subject;
        response.Data.Should().HaveCount(3);
    }
}
