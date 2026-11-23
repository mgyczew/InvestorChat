using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvestorChat.Application.Security;
using InvestorChat.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestorChat.Infrastructure.Security;

internal sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private readonly JwtOptions _options = options.Value;

    public GeneratedToken GenerateToken(UserAccount user)
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(TokenLifetime);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("username", user.UserName),
            new Claim("userId", user.Id.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new GeneratedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
