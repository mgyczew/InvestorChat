namespace InvestorChat.Domain.Entities;

public sealed class UserWatchlistEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
}
