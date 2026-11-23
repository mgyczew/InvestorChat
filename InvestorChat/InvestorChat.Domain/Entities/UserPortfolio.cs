namespace InvestorChat.Domain.Entities;

public sealed class UserPortfolio
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<PortfolioPosition> Positions { get; set; } = new List<PortfolioPosition>();
}
