using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace InvestorChat.Tests;

public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string[] SettingNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "ConnectionStrings__DefaultConnection",
        "Jwt__Key",
        "Jwt__Issuer",
        "Jwt__Audience"
    ];
    private readonly Dictionary<string, string?> _originalSettings = SettingNames.ToDictionary(name => name, Environment.GetEnvironmentVariable);
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-key-with-more-than-thirty-two-bytes");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "InvestorChat.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "InvestorChat.Tests");
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        foreach (var setting in _originalSettings)
        {
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }
}
