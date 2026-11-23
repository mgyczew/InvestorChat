using System.Net;
using InvestorChat.Server.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestorChat.Tests;

public sealed class YahooSymbolMappingTests
{
    [Theory]
    [InlineData("ASSECOBS", "ABS.WA")]
    [InlineData("BENEFIT", "BFT.WA")]
    [InlineData("DEVELIA", "DVL.WA")]
    [InlineData("FERRO", "FRO.WA")]
    [InlineData("GRUPRACUJ", "GPP.WA")]
    [InlineData("KETY", "KTY.WA")]
    [InlineData("KRUK", "KRU.WA")]
    [InlineData("NWAI", "NWA.WA")]
    [InlineData("OPONEO.PL", "OPN.WA")]
    [InlineData("PEKAO", "PEO.WA")]
    [InlineData("PKNORLEN", "PKN.WA")]
    [InlineData("RAINBOW", "RBW.WA")]
    [InlineData("TEXT", "TXT.WA")]
    [InlineData("CDR", "CDR.WA")]
    public async Task NetIncomeRequestUsesYahooSymbolForBankierTicker(string bankierSymbol, string yahooSymbol)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new NetIncomeResponseHandler();
        using var httpClient = new HttpClient(handler);
        var service = new BankierMarketDataService(httpClient, NullLogger<BankierMarketDataService>.Instance, cache);

        await service.GetNetIncomeHistoryAsync(bankierSymbol, CancellationToken.None);

        Assert.Equal(yahooSymbol, handler.LastRequestUri?.AbsolutePath.Split('/').Last());
    }

    private sealed class NetIncomeResponseHandler : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "timeseries": {
                        "result": [{
                          "annualNetIncome": [
                            { "asOfDate": "2021-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 100 } },
                            { "asOfDate": "2022-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 200 } },
                            { "asOfDate": "2023-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 300 } },
                            { "asOfDate": "2024-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 400 } },
                            { "asOfDate": "2025-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 500 } }
                          ]
                        }],
                        "error": null
                      }
                    }
                    """)
            });
        }
    }
}
