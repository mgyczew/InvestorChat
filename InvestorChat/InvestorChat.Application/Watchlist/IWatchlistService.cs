namespace InvestorChat.Application.Watchlist;

public interface IWatchlistService
{
    Task<IReadOnlyList<string>> GetSymbolsAsync(int userId, CancellationToken cancellationToken = default);
    Task ReplaceSymbolsAsync(int userId, IEnumerable<string> symbols, CancellationToken cancellationToken = default);
}
