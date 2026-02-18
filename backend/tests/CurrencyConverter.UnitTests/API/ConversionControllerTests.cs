using CurrencyConverter.API.Controllers;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.UseCases;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Domain.Models;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CurrencyConverter.UnitTests.API;

public class ConversionControllerTests
{
    private readonly Mock<ICurrencyProviderFactory> _providerFactory = new();
    private readonly Mock<IValidator<ConvertCurrencyQuery>> _validator = new();

    [Fact]
    public async Task Convert_ShouldReturnOk_WhenValidationPasses()
    {
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ConvertAsync("USD", "EUR", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionResult("USD", "EUR", 100, 85m, 0.85m, DateTime.UtcNow.Date));

        _providerFactory.Setup(f => f.GetProvider(null)).Returns(provider.Object);
        _validator.Setup(v => v.ValidateAsync(It.IsAny<ConvertCurrencyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var useCase = new ConvertCurrencyUseCase(_providerFactory.Object);
        var controller = new ConversionController(useCase, _validator.Object);

        var result = await controller.Convert("USD", "EUR", 100, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ConversionResultDto>>().Subject;
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Convert_ShouldThrowValidationException_WhenInvalid()
    {
        var failures = new List<ValidationFailure> { new("From", "Invalid") };
        _validator.Setup(v => v.ValidateAsync(It.IsAny<ConvertCurrencyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var useCase = new ConvertCurrencyUseCase(_providerFactory.Object);
        var controller = new ConversionController(useCase, _validator.Object);

        await controller.Invoking(c => c.Convert("X", "Y", 1, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();
    }
}
