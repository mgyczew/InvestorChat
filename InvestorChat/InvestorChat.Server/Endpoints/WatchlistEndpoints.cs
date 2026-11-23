using System.Security.Claims;
using InvestorChat.Application.Watchlist;
using InvestorChat.Server.Models;
using InvestorChat.Server.Services;
using Microsoft.AspNetCore.Authorization;

namespace InvestorChat.Server.Endpoints;

public static class WatchlistEndpoints
{
    public static IEndpointRouteBuilder MapWatchlistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/watchlist", GetAsync).RequireAuthorization();
        endpoints.MapPut("/api/watchlist", ReplaceAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(ClaimsPrincipal user, IWatchlistService watchlist, CancellationToken cancellationToken)
    {
        var symbols = await watchlist.GetSymbolsAsync(GetUserId(user), cancellationToken);
        return Results.Ok(symbols);
    }

    private static async Task<IResult> ReplaceAsync(WatchlistRequest request, ClaimsPrincipal user, IWatchlistService watchlist, CancellationToken cancellationToken)
    {
        if (request.Symbols is null || request.Symbols.Length > 200 || request.Symbols.Any(symbol => string.IsNullOrWhiteSpace(symbol) || !BankierMarketDataService.IsValidSymbol(symbol)))
        {
            return Results.BadRequest();
        }

        await watchlist.ReplaceSymbolsAsync(GetUserId(user), request.Symbols, cancellationToken);
        return Results.NoContent();
    }

    private static int GetUserId(ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue("userId")!);
}
