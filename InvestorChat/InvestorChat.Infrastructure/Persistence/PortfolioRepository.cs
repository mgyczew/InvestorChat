using InvestorChat.Application.Portfolios;
using InvestorChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestorChat.Infrastructure.Persistence;

internal sealed class PortfolioRepository(AppDbContext dbContext) : IPortfolioRepository
{
    public async Task<IReadOnlyList<PortfolioData>> GetForUserAsync(int userId, CancellationToken cancellationToken)
    {
        var portfolios = await dbContext.UserPortfolios
            .AsNoTracking()
            .Include(portfolio => portfolio.Positions)
            .Where(portfolio => portfolio.UserId == userId)
            .OrderBy(portfolio => portfolio.Name)
            .ToListAsync(cancellationToken);

        return portfolios.Select(ToData).ToArray();
    }

    public async Task<PortfolioData> ReplaceByNameAsync(int userId, string name, IReadOnlyList<PortfolioPositionData> positions, CancellationToken cancellationToken)
    {
        var portfolio = await dbContext.UserPortfolios
            .Include(item => item.Positions)
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Name == name, cancellationToken);

        if (portfolio is null)
        {
            portfolio = new UserPortfolio { UserId = userId, Name = name };
            dbContext.UserPortfolios.Add(portfolio);
        }
        else
        {
            dbContext.PortfolioPositions.RemoveRange(portfolio.Positions);
            portfolio.Positions.Clear();
        }

        foreach (var position in positions)
        {
            portfolio.Positions.Add(new PortfolioPosition
            {
                SecurityName = position.SecurityName,
                Exchange = position.Exchange,
                AvailableQuantity = position.AvailableQuantity,
                BlockedQuantity = position.BlockedQuantity,
                Price = position.Price,
                PriceCurrency = position.PriceCurrency,
                Value = position.Value,
                ValueCurrency = position.ValueCurrency
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToData(portfolio);
    }

    public async Task<bool> DeleteAsync(int userId, int portfolioId, CancellationToken cancellationToken)
    {
        var portfolio = await dbContext.UserPortfolios
            .SingleOrDefaultAsync(item => item.Id == portfolioId && item.UserId == userId, cancellationToken);
        if (portfolio is null)
        {
            return false;
        }

        dbContext.UserPortfolios.Remove(portfolio);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PortfolioData ToData(UserPortfolio portfolio) => new(
        portfolio.Id,
        portfolio.Name,
        portfolio.Positions
            .OrderBy(position => position.Id)
            .Select(position => new PortfolioPositionData(
                position.SecurityName,
                position.Exchange,
                position.AvailableQuantity,
                position.BlockedQuantity,
                position.Price,
                position.PriceCurrency,
                position.Value,
                position.ValueCurrency))
            .ToArray());
}
