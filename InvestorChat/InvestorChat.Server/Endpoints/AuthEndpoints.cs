using InvestorChat.Application.Auth;
using Microsoft.AspNetCore.Authorization;

namespace InvestorChat.Server.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").RequireRateLimiting("auth");
        group.MapPost("/register", async (RegisterRequest? request, IAuthService auth, CancellationToken cancellationToken) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new AuthResult(false, "Nazwa użytkownika i hasło są wymagane."));
            }
            var result = await auth.RegisterAsync(request, cancellationToken);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });
        group.MapPost("/login", async (LoginRequest? request, IAuthService auth, CancellationToken cancellationToken) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new AuthResult(false, "Nazwa użytkownika i hasło są wymagane."));
            }
            var result = await auth.LoginAsync(request, cancellationToken);
            return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);
        });
        group.MapGet("/me", [Authorize] (System.Security.Claims.ClaimsPrincipal user) =>
            Results.Ok(new { userName = user.Identity?.Name }));
        return group;
    }
}
