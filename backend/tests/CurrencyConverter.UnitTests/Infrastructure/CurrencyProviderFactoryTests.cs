using CurrencyConverter.Application.Settings;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class CurrencyProviderFactoryTests
{
    [Fact]
    public void GetProvider_ShouldReturnProvider_ByName()
    {
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var options = Options.Create(new CurrencyProviderSettings { DefaultProvider = "Frankfurter" });
        var factory = new CurrencyProviderFactory(new[] { provider.Object }, options);

        var result = factory.GetProvider("Frankfurter");

        result.Should().Be(provider.Object);
    }

    [Fact]
    public void GetProvider_ShouldReturnDefault_WhenNameIsNull()
    {
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var options = Options.Create(new CurrencyProviderSettings { DefaultProvider = "Frankfurter" });
        var factory = new CurrencyProviderFactory(new[] { provider.Object }, options);

        var result = factory.GetProvider();

        result.Should().Be(provider.Object);
    }

    [Fact]
    public void GetProvider_ShouldThrow_ForUnknownProvider()
    {
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var options = Options.Create(new CurrencyProviderSettings { DefaultProvider = "Frankfurter" });
        var factory = new CurrencyProviderFactory(new[] { provider.Object }, options);

        factory.Invoking(f => f.GetProvider("Unknown"))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Unknown*");
    }

    [Fact]
    public void GetProvider_ShouldBeCaseInsensitive()
    {
        var provider = new Mock<ICurrencyProvider>();
        provider.Setup(p => p.ProviderName).Returns("Frankfurter");

        var options = Options.Create(new CurrencyProviderSettings { DefaultProvider = "Frankfurter" });
        var factory = new CurrencyProviderFactory(new[] { provider.Object }, options);

        var result = factory.GetProvider("frankfurter");

        result.Should().Be(provider.Object);
    }
}
