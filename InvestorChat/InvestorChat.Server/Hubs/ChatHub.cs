using System.Security.Claims;
using InvestorChat.Application.Chat;
using InvestorChat.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InvestorChat.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatMessageService _messages;

    public ChatHub(IChatMessageService messages)
    {
        _messages = messages;
    }

    public async Task SendMessage(string text)
    {
        var userName = Context.User?.Identity?.Name ?? Context.User?.FindFirst("username")?.Value ?? "Anonymous";
        var trimmed = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        var message = new ChatMessage
        {
            UserName = userName,
            Content = trimmed,
            CreatedAt = DateTime.UtcNow
        };

        await _messages.AddAsync(message);
        await Clients.All.SendAsync("ReceiveMessage", userName, message.Content, message.CreatedAt.ToString("HH:mm:ss"));
    }

    public async Task<List<ChatMessage>> GetRecentMessages() =>
        (await _messages.GetRecentAsync()).ToList();
}
