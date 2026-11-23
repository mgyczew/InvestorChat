namespace InvestorChat.Application.Portfolios;

public interface IPortfolioRepository
{
    Task<IReadOnlyList<PortfolioData>> GetForUserAsync(int userId, CancellationToken cancellationToken);
    Task<PortfolioData> ReplaceByNameAsync(int userId, string name, IReadOnlyList<PortfolioPositionData> positions, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int userId, int portfolioId, CancellationToken cancellationToken);
}
