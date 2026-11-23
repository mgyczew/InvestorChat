using InvestorChat.Application.Security;
using InvestorChat.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace InvestorChat.Infrastructure.Security;

internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<UserAccount> _hasher = new();

    public string HashPassword(string password) => _hasher.HashPassword(new UserAccount(), password);

    public bool VerifyPassword(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(new UserAccount(), passwordHash, password) != PasswordVerificationResult.Failed;
}
