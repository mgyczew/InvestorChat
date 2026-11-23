using InvestorChat.Application.Auth;
using InvestorChat.Application.Chat;
using InvestorChat.Application.Portfolios;
using InvestorChat.Application.Watchlist;
using Microsoft.Extensions.DependencyInjection;

namespace InvestorChat.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IChatMessageService, ChatMessageService>();
        return services;
    }
}
