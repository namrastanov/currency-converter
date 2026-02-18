using CurrencyConverter.API.Controllers;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Settings;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace CurrencyConverter.UnitTests.API;

public class RatesControllerTests
{
    private readonly Mock<ICurrencyProviderFactory> _providerFactory = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IValidator<GetLatestRatesQuery>> _latestValidator = new();
    private readonly Mock<IValidator<GetHistoricalRatesQuery>> _historicalValidator = new();
    private readonly Mock<ICurrencyProvider> _provider = new();

    [Fact]
    public async Task GetLatestRates_ShouldReturnOk_WhenValid()
    {
        var rates = new LatestRatesDto("USD", DateTime.UtcNow.Date,
            new Dictionary<string, decimal> { ["EUR"] = 0.85m });

        _cacheService.Setup(c => c.GetAsync<LatestRatesDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        _latestValidator.Setup(v => v.ValidateAsync(It.IsAny<GetLatestRatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var cacheSettings = Options.Create(new CacheSettings());

        var latestUseCase = new GetLatestRatesUseCase(_providerFactory.Object, _cacheService.Object, cacheSettings);

        var historicalUseCase = new GetHistoricalRatesUseCase(
            _providerFactory.Object, _cacheService.Object,
            cacheSettings, TimeProvider.System);

        var controller = new RatesController(latestUseCase, historicalUseCase, _latestValidator.Object, _historicalValidator.Object);

        var result = await controller.GetLatestRates("USD", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLatestRates_ShouldThrow_WhenValidationFails()
    {
        var failures = new List<ValidationFailure> { new("Base", "Required") };
        _latestValidator.Setup(v => v.ValidateAsync(It.IsAny<GetLatestRatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var cacheSettings = Options.Create(new CacheSettings());

        var latestUseCase = new GetLatestRatesUseCase(_providerFactory.Object, _cacheService.Object, cacheSettings);

        var historicalUseCase = new GetHistoricalRatesUseCase(
            _providerFactory.Object, _cacheService.Object,
            cacheSettings, TimeProvider.System);

        var controller = new RatesController(latestUseCase, historicalUseCase, _latestValidator.Object, _historicalValidator.Object);

        await controller.Invoking(c => c.GetLatestRates("", CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetHistoricalRates_ShouldReturnOk_WithMetadata()
    {
        _historicalValidator.Setup(v => v.ValidateAsync(It.IsAny<GetHistoricalRatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var from = DateTime.UtcNow.Date.AddDays(-5);
        var to = DateTime.UtcNow.Date.AddDays(-1);

        _cacheService.Setup(c => c.GetCachedDatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<DateTime>());

        var historicalResults = new List<ExchangeRate>
        {
            new("USD", from, new Dictionary<string, decimal> { ["EUR"] = 0.85m }),
            new("USD", from.AddDays(1), new Dictionary<string, decimal> { ["EUR"] = 0.86m })
        };

        _provider.Setup(p => p.GetHistoricalRatesAsync("USD", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalResults.AsReadOnly());

        _providerFactory.Setup(f => f.GetProvider(null)).Returns(_provider.Object);

        _cacheService.Setup(c => c.GetDateRatesBatchAsync(It.IsAny<string>(), It.IsAny<IEnumerable<DateTime>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, Dictionary<string, decimal>>());

        var cacheSettings = Options.Create(new CacheSettings { GapMergeThresholdDays = 5 });

        var latestUseCase = new GetLatestRatesUseCase(_providerFactory.Object, _cacheService.Object, cacheSettings);

        var historicalUseCase = new GetHistoricalRatesUseCase(
            _providerFactory.Object, _cacheService.Object,
            cacheSettings, TimeProvider.System);

        var controller = new RatesController(latestUseCase, historicalUseCase, _latestValidator.Object, _historicalValidator.Object);

        var result = await controller.GetHistoricalRates("USD", from, to, 1, 10, 0, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<HistoricalRatesDto>>().Subject;
        response.Metadata.Should().ContainKey("totalCount");
        response.Metadata.Should().ContainKey("totalPages");
        response.Metadata.Should().ContainKey("page");
    }
}
