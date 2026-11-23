using InvestorChat.Server.Services;
using Xunit;

namespace InvestorChat.Tests;

public sealed class YahooFinancialsParserTests
{
    [Fact]
    public void ParseAnnualNetIncomeReturnsLatestFiveYearsWithReportedCurrency()
    {
        const string json = """
            {
              "timeseries": {
                "result": [{
                  "annualNetIncome": [
                    { "asOfDate": "2020-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 100 } },
                    { "asOfDate": "2021-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 200 } },
                    { "asOfDate": "2022-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 300 } },
                    { "asOfDate": "2023-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 400 } },
                    { "asOfDate": "2024-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 500 } },
                    { "asOfDate": "2025-12-31", "currencyCode": "PLN", "reportedValue": { "raw": 600 } }
                  ]
                }],
                "error": null
              }
            }
            """;

        var years = YahooFinancialsParser.ParseAnnualNetIncome(json);

        Assert.Equal(new[] { 2025, 2024, 2023, 2022, 2021 }, years.Select(year => year.FiscalYear));
        Assert.Equal(600m, years[0].Amount);
        Assert.Equal("PLN", years[0].Currency);
    }
}
