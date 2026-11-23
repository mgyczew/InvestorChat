using InvestorChat.Domain.Entities;

namespace InvestorChat.Application.Chat;

public interface IChatMessageService
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetRecentAsync(CancellationToken cancellationToken = default);
}
