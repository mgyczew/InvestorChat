using InvestorChat.Application.Persistence;
using InvestorChat.Application.Security;
using InvestorChat.Domain.Entities;

namespace InvestorChat.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokens;

    public AuthService(IUserRepository users, IPasswordHasher passwordHasher, IJwtTokenGenerator tokens)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var userName = request.UserName?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;

        if (userName.Length < 3 || userName.Length > 50 || password.Length < 8 || password.Length > 128)
        {
            return new AuthResult(false, "Nazwa użytkownika musi mieć 3–50 znaków, a hasło 8–128 znaków.");
        }

        if (email.Length == 0)
        {
            email = $"{userName}@demo.local";
        }
        else if (!email.Contains('@'))
        {
            return new AuthResult(false, "Email ma niepoprawny format.");
        }

        if (await _users.UserNameExistsAsync(userName, cancellationToken))
        {
            return new AuthResult(false, "Użytkownik o tej nazwie już istnieje.");
        }

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            return new AuthResult(false, "Adres email jest już używany.");
        }

        var user = new UserAccount
        {
            UserName = userName,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(password)
        };

        await _users.AddAsync(user, cancellationToken);

        var token = _tokens.GenerateToken(user);
        return new AuthResult(true, string.Empty, token.Value, user.UserName, token.ExpiresAtUtc);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userName = request.UserName?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (userName.Length == 0 || password.Length == 0)
        {
            return new AuthResult(false, "Nazwa użytkownika i hasło są wymagane.");
        }

        var user = await _users.FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(user.PasswordHash, password))
        {
            return new AuthResult(false, "Nieprawidłowe dane logowania.");
        }

        var token = _tokens.GenerateToken(user);
        return new AuthResult(true, string.Empty, token.Value, user.UserName, token.ExpiresAtUtc);
    }
}
