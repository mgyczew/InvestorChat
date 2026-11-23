using InvestorChat.Application.Persistence;
using InvestorChat.Domain.Entities;

namespace InvestorChat.Application.Chat;

public sealed class ChatMessageService(IChatMessageRepository repository) : IChatMessageService
{
    public Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default) =>
        repository.AddAsync(message, cancellationToken);

    public Task<IReadOnlyList<ChatMessage>> GetRecentAsync(CancellationToken cancellationToken = default) =>
        repository.GetRecentAsync(cancellationToken);
}
