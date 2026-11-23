namespace InvestorChat.Application.Persistence;

public interface IWatchlistRepository
{
    Task<IReadOnlyList<string>> GetSymbolsAsync(int userId, CancellationToken cancellationToken);
    Task ReplaceSymbolsAsync(int userId, IReadOnlyCollection<string> symbols, CancellationToken cancellationToken);
}
