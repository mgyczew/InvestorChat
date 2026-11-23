namespace InvestorChat.Application.Portfolios;

public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioData>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<PortfolioData> ImportAsync(int userId, string name, IReadOnlyList<PortfolioPositionData> positions, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int userId, int portfolioId, CancellationToken cancellationToken = default);
}
