namespace CurrencyConverter.API.Extensions;

public static class ConfigurationExtensions
{
    public static T GetRequiredSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = configuration.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException($"Configuration section '{sectionName}' is missing.");

        var settings = section.Get<T>();

        return settings ?? throw new InvalidOperationException(
            $"Configuration section '{sectionName}' could not be bound to type '{typeof(T).Name}'.");
    }
}
