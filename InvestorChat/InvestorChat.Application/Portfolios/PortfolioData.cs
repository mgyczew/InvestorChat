namespace InvestorChat.Application.Portfolios;

public sealed record PortfolioData(int Id, string Name, IReadOnlyList<PortfolioPositionData> Positions);
