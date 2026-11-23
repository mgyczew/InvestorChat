using System.Security.Claims;
using InvestorChat.Application.Portfolios;
using InvestorChat.Server.Services;

namespace InvestorChat.Server.Endpoints;

public static class PortfolioEndpoints
{
    private const long MaximumFileSize = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/portfolios", GetAsync)
            .RequireAuthorization();
        endpoints.MapDelete("/api/portfolios/{portfolioId:int}", DeleteAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/portfolios/import", ImportAsync)
            .RequireAuthorization()
            .DisableAntiforgery();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(ClaimsPrincipal user, IPortfolioService portfolios, CancellationToken cancellationToken)
    {
        var result = await portfolios.GetForUserAsync(GetUserId(user), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteAsync(int portfolioId, ClaimsPrincipal user, IPortfolioService portfolios, CancellationToken cancellationToken)
    {
        var deleted = await portfolios.DeleteAsync(GetUserId(user), portfolioId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ImportAsync(
        HttpRequest request,
        ClaimsPrincipal user,
        IPortfolioService portfolios,
        PortfolioCsvParser parser,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "Wybierz plik .csv do importu." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var name = form["name"].ToString();
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "Wybierz niepusty plik .csv." });
        }

        if (file.Length > MaximumFileSize)
        {
            return Results.BadRequest(new { message = "Plik może mieć maksymalnie 5 MB." });
        }

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Obsługiwane są wyłącznie pliki .csv." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var positions = parser.Parse(stream);
            var portfolio = await portfolios.ImportAsync(GetUserId(user), name, positions, cancellationToken);
            return Results.Ok(portfolio);
        }
        catch (PortfolioImportException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static int GetUserId(ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue("userId")!);
}
