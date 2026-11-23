using InvestorChat.Application.Persistence;
using InvestorChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestorChat.Infrastructure.Persistence;

internal sealed class WatchlistRepository(AppDbContext dbContext) : IWatchlistRepository
{
    public async Task<IReadOnlyList<string>> GetSymbolsAsync(int userId, CancellationToken cancellationToken) =>
        await dbContext.UserWatchlistEntries
            .Where(entry => entry.UserId == userId)
            .OrderBy(entry => entry.Id)
            .Select(entry => entry.Symbol)
            .ToArrayAsync(cancellationToken);

    public async Task ReplaceSymbolsAsync(int userId, IReadOnlyCollection<string> symbols, CancellationToken cancellationToken)
    {
        var entries = await dbContext.UserWatchlistEntries
            .Where(entry => entry.UserId == userId)
            .ToListAsync(cancellationToken);
        var symbolSet = symbols.ToHashSet(StringComparer.OrdinalIgnoreCase);
        dbContext.UserWatchlistEntries.RemoveRange(entries.Where(entry => !symbolSet.Contains(entry.Symbol)));
        var existingSymbols = entries.Select(entry => entry.Symbol).ToHashSet(StringComparer.OrdinalIgnoreCase);
        dbContext.UserWatchlistEntries.AddRange(symbols
            .Where(symbol => !existingSymbols.Contains(symbol))
            .Select(symbol => new UserWatchlistEntry { UserId = userId, Symbol = symbol }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
