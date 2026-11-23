using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using InvestorChat.Application.Auth;
using InvestorChat.Application.Portfolios;
using Xunit;

namespace InvestorChat.Tests;

public sealed class PortfolioApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task ImportStoresSnapshotAndReplacesPositionsForTheSamePortfolio()
    {
        using var client = await CreateAuthenticatedClientAsync();

        using var firstResponse = await ImportAsync(client, "Długoterminowy", CreateCsv("5 (2)", "CD Projekt, S.A.", "123,45", "617,25"));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstPortfolio = await firstResponse.Content.ReadFromJsonAsync<PortfolioData>();
        Assert.NotNull(firstPortfolio);
        var firstPosition = Assert.Single(firstPortfolio.Positions);
        Assert.Equal("CD Projekt, S.A.", firstPosition.SecurityName);
        Assert.Equal("GPW", firstPosition.Exchange);
        Assert.Equal(5m, firstPosition.AvailableQuantity);
        Assert.Equal(2m, firstPosition.BlockedQuantity);
        Assert.Equal(123.45m, firstPosition.Price);
        Assert.Equal("PLN", firstPosition.PriceCurrency);
        Assert.Equal(617.25m, firstPosition.Value);
        Assert.Equal("PLN", firstPosition.ValueCurrency);

        using var replacementResponse = await ImportAsync(client, "Długoterminowy", CreateCsv("8", "KGHM", "140,50", "1124,00"));
        Assert.Equal(HttpStatusCode.OK, replacementResponse.StatusCode);
        var replacement = await replacementResponse.Content.ReadFromJsonAsync<PortfolioData>();
        Assert.NotNull(replacement);
        Assert.Equal(firstPortfolio.Id, replacement.Id);
        var replacementPosition = Assert.Single(replacement.Positions);
        Assert.Equal("KGHM", replacementPosition.SecurityName);
        Assert.Equal(8m, replacementPosition.AvailableQuantity);
        Assert.Equal(0m, replacementPosition.BlockedQuantity);

        var portfolios = await client.GetFromJsonAsync<PortfolioData[]>("/api/portfolios");
        Assert.NotNull(portfolios);
        Assert.Equal("Długoterminowy", Assert.Single(portfolios).Name);
    }

    [Fact]
    public async Task PortfolioDataIsIsolatedByUser()
    {
        using var firstClient = await CreateAuthenticatedClientAsync();
        using var secondClient = await CreateAuthenticatedClientAsync();

        using var importResponse = await ImportAsync(firstClient, "Mój portfel", CreateCsv("1", "PZU", "50", "50"));
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var secondUserPortfolios = await secondClient.GetFromJsonAsync<PortfolioData[]>("/api/portfolios");
        Assert.NotNull(secondUserPortfolios);
        Assert.Empty(secondUserPortfolios);
    }

    [Fact]
    public async Task OwnerCanDeletePortfolioAndItsPositions()
    {
        using var client = await CreateAuthenticatedClientAsync();
        using var importResponse = await ImportAsync(client, "Do usunięcia", CreateCsv("1", "PZU", "50", "50"));
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        var portfolio = await importResponse.Content.ReadFromJsonAsync<PortfolioData>();
        Assert.NotNull(portfolio);

        using var deleteResponse = await client.DeleteAsync($"/api/portfolios/{portfolio.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var portfolios = await client.GetFromJsonAsync<PortfolioData[]>("/api/portfolios");
        Assert.NotNull(portfolios);
        Assert.Empty(portfolios);
    }

    [Fact]
    public async Task UserCannotDeleteAnotherUsersPortfolio()
    {
        using var ownerClient = await CreateAuthenticatedClientAsync();
        using var otherClient = await CreateAuthenticatedClientAsync();
        using var importResponse = await ImportAsync(ownerClient, "Cudzy portfel", CreateCsv("1", "PZU", "50", "50"));
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        var portfolio = await importResponse.Content.ReadFromJsonAsync<PortfolioData>();
        Assert.NotNull(portfolio);

        using var deleteResponse = await otherClient.DeleteAsync($"/api/portfolios/{portfolio.Id}");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        var portfolios = await ownerClient.GetFromJsonAsync<PortfolioData[]>("/api/portfolios");
        Assert.NotNull(portfolios);
        Assert.Single(portfolios);
    }

    [Fact]
    public async Task ImportRejectsCsvWithMissingColumns()
    {
        using var client = await CreateAuthenticatedClientAsync();

        using var response = await ImportAsync(client, "Błędny", CreateCsv("1", "PZU", "50", "50", includeAllHeaders: false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PortfolioEndpointsRequireAuthentication()
    {
        using var client = factory.CreateClient();

        using var getResponse = await client.GetAsync("/api/portfolios");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        using var importResponse = await ImportAsync(client, "Portfel", CreateCsv("1", "PZU", "50", "50"));
        Assert.Equal(HttpStatusCode.Unauthorized, importResponse.StatusCode);
    }

    [Theory]
    [InlineData(';')]
    [InlineData(',')]
    [InlineData('\t')]
    public async Task ImportAcceptsCommonCsvDelimiters(char delimiter)
    {
        using var client = await CreateAuthenticatedClientAsync();

        using var response = await ImportAsync(client, "CSV", CreateCsv("3", "Akcja, Inc.", "12,50", "37,50", delimiter: delimiter));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioData>();
        Assert.Equal("Akcja, Inc.", Assert.Single(portfolio!.Positions).SecurityName);
    }

    [Fact]
    public async Task ImportAcceptsWindows1250CsvWithPolishCharacters()
    {
        using var client = await CreateAuthenticatedClientAsync();

        using var response = await ImportAsync(client, "Polski CSV", CreateWindows1250Csv());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioData>();
        Assert.Equal("Łódź", Assert.Single(portfolio!.Positions).SecurityName);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var userName = $"portfolio{Guid.NewGuid():N}";
        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            Password = "strong-password-123"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(result?.Token);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        return client;
    }

    private static async Task<HttpResponseMessage> ImportAsync(HttpClient client, string name, byte[] csv)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new ByteArrayContent(csv), "file", "portfel.CSV");
        return await client.PostAsync("/api/portfolios/import", content);
    }

    private static byte[] CreateCsv(string quantity, string paper, string price, string value, bool includeAllHeaders = true, char delimiter = ';')
    {
        var headers = includeAllHeaders
            ? new[] { "Papier", "Giełda", "Liczba dostępna (Blokady)", "Kurs", "Waluta", "Wartość", "Waluta" }
            : new[] { "Papier", "Giełda", "Kurs", "Wartość" };
        var values = includeAllHeaders
            ? new[] { paper, "GPW", quantity, price, "PLN", value, "PLN" }
            : new[] { paper, "GPW", price, value };
        var records = new[] { headers, values }
            .Select(record => string.Join(delimiter.ToString(), record.Select(EscapeCsv)));
        var bytes = Encoding.UTF8.GetBytes(string.Join("\r\n", records));
        return Encoding.UTF8.GetPreamble().Concat(bytes).ToArray();
    }

    private static byte[] CreateWindows1250Csv()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1250);
        return encoding.GetBytes("Papier;Giełda;Liczba dostępna (Blokady);Kurs;Waluta;Wartość;Waluta\r\nŁódź;GPW;1 (0);15,50;PLN;15,50;PLN");
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
