using System.Diagnostics.CodeAnalysis;
using CurrencyConverter.Application.UseCases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetCurrenciesUseCase>();
        services.AddScoped<GetLatestRatesUseCase>();
        services.AddScoped<ConvertCurrencyUseCase>();
        services.AddScoped<GetHistoricalRatesUseCase>();

        services.AddScoped<LoginUseCase>();
        services.AddScoped<RegisterUseCase>();
        services.AddScoped<GetAllUsersUseCase>();
        services.AddScoped<GetUserByIdUseCase>();
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<UpdateUserRoleUseCase>();
        services.AddScoped<DeleteUserUseCase>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
