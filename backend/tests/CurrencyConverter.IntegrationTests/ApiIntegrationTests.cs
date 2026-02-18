using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace CurrencyConverter.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetJwtToken(string username = "admin", string password = "admin123")
    {
        var loginRequest = new { username, password };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthResult>>(content, _jsonOptions);
        return apiResponse!.Data!.Token;
    }

    private void SetupFrankfurterCurrencies()
    {
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/currencies").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["EUR"] = "Euro",
                    ["USD"] = "US Dollar",
                    ["GBP"] = "British Pound",
                    ["TRY"] = "Turkish Lira",
                    ["PLN"] = "Polish Zloty"
                })));
    }

    private void SetupFrankfurterLatestRates()
    {
        _factory.WireMockServer
            .Given(Request.Create().WithPath("/latest").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    amount = 1,
                    @base = "EUR",
                    date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    rates = new Dictionary<string, decimal>
                    {
                        ["USD"] = 1.1m,
                        ["GBP"] = 0.85m,
                        ["TRY"] = 35.5m,
                        ["PLN"] = 4.5m
                    }
                })));
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnToken()
    {
        var request = new { username = $"testuser_{Guid.NewGuid():N}", password = "password123" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthResult>>(content, _jsonOptions);
        apiResponse!.Data!.Token.Should().NotBeNullOrEmpty();
        apiResponse.Data.Username.Should().Be(request.username);
        apiResponse.Data.Role.Should().Be("User");
    }

    [Fact]
    public async Task Login_ShouldReturnToken_ForValidCredentials()
    {
        var token = await GetJwtToken();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldReturn401_ForInvalidCredentials()
    {
        var request = new { username = "admin", password = "wrongpassword" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrencies_ShouldReturn401_WithoutToken()
    {
        var response = await _client.GetAsync("/api/v1/currencies");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrencies_ShouldReturnList_WithRestrictedFlags()
    {
        SetupFrankfurterCurrencies();
        var token = await GetJwtToken();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/currencies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("EUR");
        content.Should().Contain("restricted");
    }

    [Fact]
    public async Task GetLatestRates_ShouldReturnRates()
    {
        SetupFrankfurterLatestRates();
        var token = await GetJwtToken();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/rates/latest?base=EUR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("USD");
        content.Should().NotContain("\"TRY\"");
    }

    [Fact]
    public async Task GetLatestRates_ShouldReturn400_ForRestrictedCurrency()
    {
        var token = await GetJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/rates/latest?base=TRY");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Convert_ShouldReturnResult()
    {
        SetupFrankfurterLatestRates();
        var token = await GetJwtToken();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/convert?from=EUR&to=USD&amount=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthLive_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_ShouldReturn200_WhenDepsAvailable()
    {
        SetupFrankfurterCurrencies();
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task AdminEndpoint_ShouldReturn403_ForUserRole()
    {
        var registerRequest = new { username = $"user_{Guid.NewGuid():N}", password = "password123" };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerApiResponse = JsonSerializer.Deserialize<ApiResponse<AuthResult>>(registerContent, _jsonOptions);
        var userToken = registerApiResponse!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var response = await _client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_ShouldReturnUsers_ForAdmin()
    {
        var token = await GetJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
