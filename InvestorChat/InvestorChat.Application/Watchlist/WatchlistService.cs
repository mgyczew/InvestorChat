using InvestorChat.Application.Persistence;

namespace InvestorChat.Application.Watchlist;

public sealed class WatchlistService(IWatchlistRepository repository) : IWatchlistService
{
    public Task<IReadOnlyList<string>> GetSymbolsAsync(int userId, CancellationToken cancellationToken = default) =>
        repository.GetSymbolsAsync(userId, cancellationToken);

    public Task ReplaceSymbolsAsync(int userId, IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        var normalized = symbols
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return repository.ReplaceSymbolsAsync(userId, normalized, cancellationToken);
    }
}
