using InvestorChat.Application.Persistence;
using InvestorChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestorChat.Infrastructure.Persistence;

internal sealed class ChatMessageRepository(AppDbContext dbContext) : IChatMessageRepository
{
    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(CancellationToken cancellationToken) =>
        await dbContext.ChatMessages
            .OrderByDescending(message => message.CreatedAt)
            .Take(20)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
}
