namespace InvestorChat.Application.Portfolios;

public sealed record PortfolioPositionData(
    string SecurityName,
    string Exchange,
    decimal AvailableQuantity,
    decimal BlockedQuantity,
    decimal Price,
    string PriceCurrency,
    decimal Value,
    string ValueCurrency);
