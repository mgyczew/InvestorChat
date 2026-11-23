using InvestorChat.Domain.Entities;

namespace InvestorChat.Application.Persistence;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> GetRecentAsync(CancellationToken cancellationToken);
}
