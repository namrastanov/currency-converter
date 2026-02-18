using Microsoft.AspNetCore.Http;

namespace CurrencyConverter.Infrastructure.Http;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_httpContextAccessor.HttpContext?.Items.TryGetValue("CorrelationId", out var correlationId) == true
            && correlationId is string id)
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", id);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
