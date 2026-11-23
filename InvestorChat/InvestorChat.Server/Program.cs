using System.Threading.RateLimiting;
using InvestorChat.Application;
using InvestorChat.Infrastructure;
using InvestorChat.Infrastructure.Persistence;
using InvestorChat.Server.Endpoints;
using InvestorChat.Server.Hubs;
using InvestorChat.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PortfolioCsvParser>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("ClientPolicy", policy =>
{
    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins);
    }
}));

builder.Services.AddSignalR();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddHttpClient<MarketPriceService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("InvestorChat/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.Referrer = new Uri("https://finance.yahoo.com/");
});
builder.Services.AddHttpClient<IMarketDataService, BankierMarketDataService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/131.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHostedService<MarketPriceService>();
builder.Services.AddProblemDetails();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseCors("ClientPolicy");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

await app.Services.MigrateDatabaseAsync();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapMarketEndpoints();
app.MapAuthEndpoints();
app.MapWatchlistEndpoints();
app.MapPortfolioEndpoints();
app.MapHub<ChatHub>("/chathub").RequireAuthorization();
app.MapHub<MarketHub>("/markethub");
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
