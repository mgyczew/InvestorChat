using InvestorChat.Server.Models;

namespace InvestorChat.Server.Services;

public interface IMarketDataService
{
    Task<MarketQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken);

    Task<DividendResponse> GetDividendAsync(string symbol, int currentYear, CancellationToken cancellationToken);

    Task<NetIncomeHistoryResponse> GetNetIncomeHistoryAsync(string symbol, CancellationToken cancellationToken);
}
