using InvestorChat.Application.Persistence;

namespace InvestorChat.Application.Portfolios;

public sealed class PortfolioService(IPortfolioRepository repository) : IPortfolioService
{
    public Task<IReadOnlyList<PortfolioData>> GetForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        repository.GetForUserAsync(userId, cancellationToken);

    public Task<PortfolioData> ImportAsync(int userId, string name, IReadOnlyList<PortfolioPositionData> positions, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length is 0 or > 100)
        {
            throw new ArgumentException("Nazwa portfela musi mieć od 1 do 100 znaków.", nameof(name));
        }

        if (positions.Count is 0 or > 2000)
        {
            throw new ArgumentException("Portfel musi zawierać od 1 do 2000 pozycji.", nameof(positions));
        }

        return repository.ReplaceByNameAsync(userId, normalizedName, positions, cancellationToken);
    }

    public Task<bool> DeleteAsync(int userId, int portfolioId, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(userId, portfolioId, cancellationToken);
}
