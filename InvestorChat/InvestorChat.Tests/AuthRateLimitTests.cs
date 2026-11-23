using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace InvestorChat.Tests;

public sealed class AuthRateLimitTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task LoginRateLimitRejectsExcessiveRequests()
    {
        using var client = factory.CreateClient();
        HttpResponseMessage? lastResponse = null;

        for (var attempt = 0; attempt < 11; attempt++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", new { userName = "missing-user", password = "wrong-password" });
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }
}
