namespace InvestorChat.Domain.Entities;

public sealed class PortfolioPosition
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public string SecurityName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal BlockedQuantity { get; set; }
    public decimal Price { get; set; }
    public string PriceCurrency { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string ValueCurrency { get; set; } = string.Empty;
}
