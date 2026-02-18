using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CurrencyConverter.API.HealthChecks;

public class FrankfurterHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FrankfurterHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("FrankfurterHealthCheck");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await client.GetAsync("/v1/currencies", cts.Token);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Frankfurter API is reachable.")
                : HealthCheckResult.Unhealthy($"Frankfurter API returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Frankfurter API is unreachable.", ex);
        }
    }
}
