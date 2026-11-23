using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using InvestorChat.Server.Models;
using Microsoft.Extensions.Caching.Memory;

namespace InvestorChat.Server.Services;

public sealed class BankierMarketDataService(
    HttpClient httpClient,
    ILogger<BankierMarketDataService> logger,
    IMemoryCache memoryCache) : IMarketDataService
{
    private static readonly Regex SymbolPattern = new("^[A-Za-z0-9.-]{1,15}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PricePattern = new("o-quotes-profile-header-box__price.*?a-quote-item -value[^>]*>\\s*([^<]+)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ChangePattern = new("o-quotes-profile-header-box__change.*?a-quote-item -value-change[^>]*>\\s*([^<]+)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex TableRowPattern = new(@"<tr\b[^>]*>(.*?)</tr>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex TableCellPattern = new(@"<td\b[^>]*>(.*?)</td>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex HtmlTagPattern = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly string[] YahooHosts = ["query1.finance.yahoo.com", "query2.finance.yahoo.com"];
    private static readonly IReadOnlyDictionary<string, string> YahooSymbolOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ASSECOBS"] = "ABS",
        ["BENEFIT"] = "BFT",
        ["DEVELIA"] = "DVL",
        ["FERRO"] = "FRO",
        ["GRUPRACUJ"] = "GPP",
        ["KETY"] = "KTY",
        ["KRUK"] = "KRU",
        ["NWAI"] = "NWA",
        ["OPONEO.PL"] = "OPN",
        ["PEKAO"] = "PEO",
        ["PEAKO"] = "PEO",
        ["PKNORLEN"] = "PKN",
        ["RAINBOW"] = "RBW",
        ["TEXT"] = "TXT"
    };

    public static bool IsValidSymbol(string symbol) => SymbolPattern.IsMatch(symbol);

    private static string GetYahooSymbol(string symbol) =>
        $"{(YahooSymbolOverrides.TryGetValue(symbol, out var yahooSymbol) ? yahooSymbol : symbol)}.WA";

    public async Task<MarketQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var bankierSymbol = normalizedSymbol == "PEAKO" ? "PEO" : normalizedSymbol;
        var url = $"https://www.bankier.pl/inwestowanie/profile/quote.html?symbol={Uri.EscapeDataString(bankierSymbol)}";
        var html = await httpClient.GetStringAsync(url, cancellationToken);
        var priceMatch = PricePattern.Match(html);
        var changeMatch = ChangePattern.Match(html);

        if (!priceMatch.Success || !changeMatch.Success ||
            !TryParseBankierAmount(priceMatch.Groups[1].Value, out var price) ||
            !TryParseBankierAmount(changeMatch.Groups[1].Value, out var change))
        {
            logger.LogInformation("No quote available for symbol {Symbol}", normalizedSymbol);
            return null;
        }

        return new MarketQuoteResponse(normalizedSymbol, price, change, DateTimeOffset.UtcNow);
    }

    public async Task<DividendResponse> GetDividendAsync(
        string symbol,
        int currentYear,
        CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        DividendResponse? biznesRadarResponse = null;

        try
        {
            biznesRadarResponse = await GetBiznesRadarDividendAsync(normalizedSymbol, currentYear, cancellationToken);
            if (biznesRadarResponse.AmountPlnPerShare.HasValue)
            {
                return biznesRadarResponse;
            }
        }

        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "BiznesRadar dividend data unavailable for {Symbol}", normalizedSymbol);
        }

        try
        {
            var yahooResponse = await GetYahooDividendAsync(normalizedSymbol, currentYear, cancellationToken);
            return yahooResponse.AmountPlnPerShare.HasValue
                ? yahooResponse
                : biznesRadarResponse ?? yahooResponse;
        }
        catch (HttpRequestException exception) when (biznesRadarResponse is not null)
        {
            logger.LogWarning(exception, "Yahoo dividend fallback unavailable for {Symbol}", normalizedSymbol);
            return biznesRadarResponse;
        }
    }

    public async Task<NetIncomeHistoryResponse> GetNetIncomeHistoryAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var response = await memoryCache.GetOrCreateAsync(
            $"annual-net-income:{normalizedSymbol}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return await GetYahooNetIncomeHistoryAsync(normalizedSymbol, cancellationToken);
            });

        return response ?? new NetIncomeHistoryResponse(normalizedSymbol, []);
    }

    private async Task<NetIncomeHistoryResponse> GetYahooNetIncomeHistoryAsync(string symbol, CancellationToken cancellationToken)
    {
        var yahooSymbol = GetYahooSymbol(symbol);
        var startYear = DateTime.UtcNow.Year - 7;
        var endYear = DateTime.UtcNow.Year + 1;
        HttpRequestException? lastError = null;

        foreach (var host in YahooHosts)
        {
            try
            {
                var years = await GetYahooAnnualNetIncomeAsync(host, yahooSymbol, startYear, endYear, cancellationToken);
                if (years.Count == 0)
                {
                    continue;
                }

                if (years.Count < 5)
                {
                    var oldestYear = years[^1].FiscalYear;
                    try
                    {
                        var olderYears = await GetYahooAnnualNetIncomeAsync(
                            host,
                            yahooSymbol,
                            oldestYear - 1,
                            oldestYear,
                            cancellationToken);
                        years = years
                            .Concat(olderYears)
                            .OrderByDescending(year => year.FiscalYear)
                            .DistinctBy(year => year.FiscalYear)
                            .Take(5)
                            .ToArray();
                    }
                    catch (HttpRequestException exception)
                    {
                        logger.LogWarning(exception, "Older Yahoo net income data unavailable for {Symbol}", symbol);
                    }
                    catch (JsonException exception)
                    {
                        logger.LogWarning(exception, "Older Yahoo net income data was invalid for {Symbol}", symbol);
                    }
                }

                return new NetIncomeHistoryResponse(symbol, years);
            }
            catch (HttpRequestException exception)
            {
                lastError = exception;
            }
            catch (JsonException exception)
            {
                lastError = new HttpRequestException("Yahoo Finance returned invalid net income data.", exception);
            }
        }

        throw lastError ?? new HttpRequestException("Yahoo Finance net income data is unavailable.");
    }

    private async Task<IReadOnlyList<NetIncomeYearResponse>> GetYahooAnnualNetIncomeAsync(
        string host,
        string yahooSymbol,
        int startYear,
        int endYear,
        CancellationToken cancellationToken)
    {
        var period1 = new DateTimeOffset(startYear, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var period2 = new DateTimeOffset(endYear, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"https://{host}/ws/fundamentals-timeseries/v1/finance/timeseries/{Uri.EscapeDataString(yahooSymbol)}?symbol={Uri.EscapeDataString(yahooSymbol)}&type=annualNetIncome&merge=false&period1={period1}&period2={period2}";
        var json = await httpClient.GetStringAsync(url, cancellationToken);
        return YahooFinancialsParser.ParseAnnualNetIncome(json);
    }

    private async Task<DividendResponse> GetBiznesRadarDividendAsync(
        string symbol,
        int currentYear,
        CancellationToken cancellationToken)
    {
        var providerSymbol = symbol == "PEAKO" ? "PEO" : symbol;
        var html = await httpClient.GetStringAsync(
            $"https://www.biznesradar.pl/dywidenda/{Uri.EscapeDataString(providerSymbol)}",
            cancellationToken);
        var dividendEvents = new List<DividendEvent>();

        foreach (Match row in TableRowPattern.Matches(html))
        {
            var cells = TableCellPattern.Matches(row.Groups[1].Value)
                .Select(match => CleanHtmlText(match.Groups[1].Value))
                .ToArray();

            if (cells.Length < 10 || !int.TryParse(cells[0], NumberStyles.None, CultureInfo.InvariantCulture, out var fiscalYear) ||
                !TryParseDividendAmount(cells[2], out var amount))
            {
                continue;
            }

            var calendarYear = fiscalYear;
            DateTimeOffset? referenceDate = null;
            var referenceDateLabel = "Rok obrotowy";

            if (TryParseDate(cells[8], out var entitlementDate))
            {
                referenceDate = entitlementDate;
                calendarYear = entitlementDate.Year;
                referenceDateLabel = "Ostatni dzień notowania z prawem do dywidendy";
            }
            else if (TryParseDate(cells[9], out var payoutDate))
            {
                referenceDate = payoutDate;
                calendarYear = payoutDate.Year;
                referenceDateLabel = "Dzień wypłaty";
            }

            dividendEvents.Add(new DividendEvent(calendarYear, amount, referenceDate, referenceDateLabel));
        }

        return SelectDividend(symbol, currentYear, dividendEvents, "BiznesRadar");
    }

    private async Task<DividendResponse> GetYahooDividendAsync(
        string symbol,
        int currentYear,
        CancellationToken cancellationToken)
    {
        var yahooSymbol = GetYahooSymbol(symbol);
        HttpRequestException? lastError = null;

        foreach (var host in YahooHosts)
        {
            try
            {
                var url = $"https://{host}/v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?range=5y&interval=1d&events=div";
                var json = await httpClient.GetStringAsync(url, cancellationToken);
                return ParseYahooDividendResponse(symbol, currentYear, json);
            }
            catch (HttpRequestException exception)
            {
                lastError = exception;
            }
            catch (JsonException exception)
            {
                lastError = new HttpRequestException("Yahoo Finance returned invalid dividend data.", exception);
            }
        }

        throw lastError ?? new HttpRequestException("Yahoo Finance dividend data is unavailable.");
    }

    private static DividendResponse ParseYahooDividendResponse(string symbol, int currentYear, string json)
    {
        using var document = JsonDocument.Parse(json);
        var chart = document.RootElement.GetProperty("chart");
        if (chart.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
        {
            throw new HttpRequestException("Yahoo Finance returned an error for the requested symbol.");
        }

        var result = chart.GetProperty("result");
        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
        {
            return NoDividendData(symbol, currentYear, "Yahoo Finance");
        }

        var quote = result[0];
        if (!quote.TryGetProperty("events", out var events) ||
            !events.TryGetProperty("dividends", out var dividends))
        {
            return NoDividendData(symbol, currentYear, "Yahoo Finance");
        }

        var dividendEvents = new List<DividendEvent>();
        foreach (var dividend in dividends.EnumerateObject())
        {
            if (!dividend.Value.TryGetProperty("amount", out var amountElement) ||
                !amountElement.TryGetDecimal(out var amount) || amount <= 0)
            {
                continue;
            }

            var unixTime = dividend.Value.TryGetProperty("date", out var dateElement)
                ? dateElement.GetInt64()
                : long.Parse(dividend.Name, CultureInfo.InvariantCulture);
            var date = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            dividendEvents.Add(new DividendEvent(date.Year, amount, date, "Dzień bez prawa do dywidendy"));
        }

        return SelectDividend(symbol, currentYear, dividendEvents, "Yahoo Finance");
    }

    private static DividendResponse SelectDividend(
        string symbol,
        int currentYear,
        List<DividendEvent> events,
        string source)
    {
        var selectedEvents = events.Where(item => item.CalendarYear == currentYear).ToArray();
        var dividendYear = currentYear;
        var isPreviousYear = false;

        if (selectedEvents.Length == 0)
        {
            dividendYear = currentYear - 1;
            isPreviousYear = true;
            selectedEvents = events.Where(item => item.CalendarYear == dividendYear).ToArray();
        }

        if (selectedEvents.Length == 0)
        {
            return NoDividendData(symbol, currentYear, source);
        }

        var latestEvent = selectedEvents
            .OrderByDescending(item => item.ReferenceDate ?? DateTimeOffset.MinValue)
            .First();

        return new DividendResponse(
            symbol,
            selectedEvents.Sum(item => item.Amount),
            currentYear,
            dividendYear,
            isPreviousYear,
            latestEvent.ReferenceDate,
            latestEvent.ReferenceDateLabel,
            source);
    }

    private static DividendResponse NoDividendData(string symbol, int currentYear, string source) =>
        new(symbol, null, currentYear, null, false, null, null, source);

    private static string CleanHtmlText(string value) =>
        Regex.Replace(WebUtility.HtmlDecode(HtmlTagPattern.Replace(value, string.Empty)), @"\s+", " ").Trim();

    private static bool TryParseDividendAmount(string value, out decimal amount)
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) && amount > 0;
    }

    private static bool TryParseDate(string value, out DateTimeOffset date)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            date = new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            return true;
        }

        date = default;
        return false;
    }

    private static bool TryParseBankierAmount(string value, out decimal amount)
    {
        var normalized = value
            .Replace("zł", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\u00a0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private sealed record DividendEvent(
        int CalendarYear,
        decimal Amount,
        DateTimeOffset? ReferenceDate,
        string? ReferenceDateLabel);
}
