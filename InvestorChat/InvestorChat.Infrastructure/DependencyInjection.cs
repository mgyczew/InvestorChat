using System.Security.Claims;
using System.Text;
using InvestorChat.Application.Persistence;
using InvestorChat.Application.Portfolios;
using InvestorChat.Application.Security;
using InvestorChat.Infrastructure.Persistence;
using InvestorChat.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InvestorChat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required. Set it in User Secrets or the ConnectionStrings__DefaultConnection environment variable.");
        }
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is required.");
        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32 || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
        {
            throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes, and Jwt:Issuer and Jwt:Audience are required.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    NameClaimType = ClaimTypes.Name,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/chathub") && context.Request.Query.TryGetValue("access_token", out var token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();

        return services;
    }
}
