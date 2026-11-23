using InvestorChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestorChat.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserWatchlistEntry> UserWatchlistEntries => Set<UserWatchlistEntry>();
    public DbSet<UserPortfolio> UserPortfolios => Set<UserPortfolio>();
    public DbSet<PortfolioPosition> PortfolioPositions => Set<PortfolioPosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(user => user.UserName).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.UserName).HasMaxLength(50);
            entity.Property(user => user.Email).HasMaxLength(200);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(message => message.UserName).HasMaxLength(50);
            entity.Property(message => message.Content).HasMaxLength(1000);
        });

        modelBuilder.Entity<UserWatchlistEntry>(entity =>
        {
            entity.HasIndex(entry => new { entry.UserId, entry.Symbol }).IsUnique();
            entity.Property(entry => entry.Symbol).HasMaxLength(15);
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPortfolio>(entity =>
        {
            entity.HasIndex(portfolio => new { portfolio.UserId, portfolio.Name }).IsUnique();
            entity.Property(portfolio => portfolio.Name).HasMaxLength(100);
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(portfolio => portfolio.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(portfolio => portfolio.Positions)
                .WithOne()
                .HasForeignKey(position => position.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PortfolioPosition>(entity =>
        {
            entity.HasIndex(position => position.PortfolioId);
            entity.Property(position => position.SecurityName).HasMaxLength(200);
            entity.Property(position => position.Exchange).HasMaxLength(100);
            entity.Property(position => position.AvailableQuantity).HasPrecision(20, 6);
            entity.Property(position => position.BlockedQuantity).HasPrecision(20, 6);
            entity.Property(position => position.Price).HasPrecision(20, 6);
            entity.Property(position => position.PriceCurrency).HasMaxLength(10);
            entity.Property(position => position.Value).HasPrecision(20, 6);
            entity.Property(position => position.ValueCurrency).HasMaxLength(10);
        });
    }
}
