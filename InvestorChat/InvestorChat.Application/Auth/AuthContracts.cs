namespace InvestorChat.Application.Auth;

public sealed class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record AuthResult(bool Success, string Message, string? Token = null, string? UserName = null, DateTimeOffset? ExpiresAtUtc = null);
