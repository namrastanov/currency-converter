using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.UseCases;

public class ConvertCurrencyUseCaseTests
{
    private readonly Mock<ICurrencyProviderFactory> _providerFactory = new();
    private readonly Mock<ICurrencyProvider> _provider = new();
    private readonly ConvertCurrencyUseCase _useCase;

    public ConvertCurrencyUseCaseTests()
    {
        _providerFactory.Setup(f => f.GetProvider(null)).Returns(_provider.Object);
        _useCase = new ConvertCurrencyUseCase(_providerFactory.Object);
    }

    [Fact]
    public async Task Should_ThrowCurrencyNotSupportedException_WhenFromIsRestricted()
    {
        var query = new ConvertCurrencyQuery("TRY", "USD", 100);

        await _useCase.Invoking(u => u.ExecuteAsync(query))
            .Should().ThrowAsync<CurrencyNotSupportedException>();
    }

    [Fact]
    public async Task Should_ThrowCurrencyNotSupportedException_WhenToIsRestricted()
    {
        var query = new ConvertCurrencyQuery("EUR", "PLN", 100);

        await _useCase.Invoking(u => u.ExecuteAsync(query))
            .Should().ThrowAsync<CurrencyNotSupportedException>();
    }

    [Fact]
    public async Task Should_ReturnConversionResult_OnHappyPath()
    {
        var conversionResult = new ConversionResult("EUR", "USD", 100, 110, 1.1m, DateTime.UtcNow);
        _provider.Setup(p => p.ConvertAsync("EUR", "USD", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversionResult);

        var result = await _useCase.ExecuteAsync(new ConvertCurrencyQuery("EUR", "USD", 100));

        result.From.Should().Be("EUR");
        result.To.Should().Be("USD");
        result.Amount.Should().Be(100);
        result.Result.Should().Be(110);
        result.Rate.Should().Be(1.1m);
    }
}
