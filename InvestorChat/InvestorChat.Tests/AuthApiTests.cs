using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InvestorChat.Application.Auth;
using Xunit;

namespace InvestorChat.Tests;

public sealed class AuthApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task RegisteredUserCanAuthenticateAndPersistWatchlist()
    {
        using var client = factory.CreateClient();
        var userName = $"user{Guid.NewGuid():N}";
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            Password = "strong-password-123"
        });

        Assert.True(registerResponse.StatusCode == HttpStatusCode.OK, await registerResponse.Content.ReadAsStringAsync());
        var result = await registerResponse.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.True(result.ExpiresAtUtc > DateTimeOffset.UtcNow);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        var saveResponse = await client.PutAsJsonAsync("/api/watchlist", new { symbols = new[] { "PKN", "CDR" } });
        Assert.Equal(HttpStatusCode.NoContent, saveResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UserName = userName,
            Password = "strong-password-123"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(loginResult?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        var savedSymbols = await client.GetFromJsonAsync<string[]>("/api/watchlist");
        Assert.Equal(new[] { "PKN", "CDR" }, savedSymbols);
    }

    [Fact]
    public async Task LoginWithWrongPasswordReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        var userName = $"user{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            Password = "strong-password-123"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UserName = userName,
            Password = "incorrect-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationRejectsMissingFieldsWithoutServerError()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { userName = (string?)null, password = "abc123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedWatchlistRequiresJwt()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/watchlist");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NewUserHasAnEmptyWatchlist()
    {
        using var client = factory.CreateClient();
        var userName = $"user{Guid.NewGuid():N}";
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            Password = "strong-password-123"
        });
        var result = await registerResponse.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(result?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        var symbols = await client.GetFromJsonAsync<string[]>("/api/watchlist");

        Assert.NotNull(symbols);
        Assert.Empty(symbols);
    }
}
