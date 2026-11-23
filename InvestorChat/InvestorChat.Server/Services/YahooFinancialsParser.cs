using System.Globalization;
using System.Text.Json;
using InvestorChat.Server.Models;

namespace InvestorChat.Server.Services;

public static class YahooFinancialsParser
{
    public static IReadOnlyList<NetIncomeYearResponse> ParseAnnualNetIncome(string json, int maximumYears = 5)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("timeseries", out var timeseries))
        {
            return [];
        }

        if (timeseries.TryGetProperty("error", out var error) && error.ValueKind is not JsonValueKind.Null)
        {
            throw new HttpRequestException("Yahoo Finance returned an error for the requested financial data.");
        }

        if (!timeseries.TryGetProperty("result", out var results) || results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
        {
            return [];
        }

        var firstResult = results[0];
        if (!firstResult.TryGetProperty("annualNetIncome", out var annualNetIncome) || annualNetIncome.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var years = new List<NetIncomeYearResponse>();
        foreach (var item in annualNetIncome.EnumerateArray())
        {
            if (!item.TryGetProperty("asOfDate", out var dateElement) ||
                !DateOnly.TryParseExact(dateElement.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOfDate) ||
                !item.TryGetProperty("reportedValue", out var reportedValue) ||
                !reportedValue.TryGetProperty("raw", out var rawValue) ||
                !TryGetDecimal(rawValue, out var amount))
            {
                continue;
            }

            var currency = item.TryGetProperty("currencyCode", out var currencyElement)
                ? currencyElement.GetString() ?? string.Empty
                : string.Empty;
            years.Add(new NetIncomeYearResponse(asOfDate.Year, amount, currency));
        }

        return years
            .OrderByDescending(year => year.FiscalYear)
            .DistinctBy(year => year.FiscalYear)
            .Take(Math.Max(0, maximumYears))
            .ToArray();
    }

    private static bool TryGetDecimal(JsonElement value, out decimal amount)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetDecimal(out amount);
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        amount = default;
        return false;
    }
}
