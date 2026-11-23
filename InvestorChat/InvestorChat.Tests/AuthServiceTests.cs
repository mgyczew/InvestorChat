using InvestorChat.Application.Auth;
using InvestorChat.Application.Persistence;
using InvestorChat.Application.Security;
using InvestorChat.Domain.Entities;
using Xunit;

namespace InvestorChat.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_HashesPasswordAndReturnsToken()
    {
        var users = new InMemoryUserRepository();
        var service = new AuthService(users, new FakePasswordHasher(), new FakeTokenGenerator());

        var result = await service.RegisterAsync(new RegisterRequest
        {
            UserName = "alice",
            Email = "alice@example.test",
            Password = "strong-pass-123"
        });

        Assert.True(result.Success);
        Assert.Equal("test-token", result.Token);
        Assert.Equal("hashed:strong-pass-123", Assert.Single(users.Users).PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_RejectsShortPassword()
    {
        var users = new InMemoryUserRepository();
        var service = new AuthService(users, new FakePasswordHasher(), new FakeTokenGenerator());

        var result = await service.RegisterAsync(new RegisterRequest
        {
            UserName = "alice",
            Email = "alice@example.test",
            Password = "short"
        });

        Assert.False(result.Success);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task LoginAsync_RejectsWrongPassword()
    {
        var users = new InMemoryUserRepository();
        users.Users.Add(new UserAccount { UserName = "alice", PasswordHash = "hashed:right-password" });
        var service = new AuthService(users, new FakePasswordHasher(), new FakeTokenGenerator());

        var result = await service.LoginAsync(new LoginRequest { UserName = "alice", Password = "wrong-password" });

        Assert.False(result.Success);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task RegisterAsync_RejectsOverlongPassword()
    {
        var users = new InMemoryUserRepository();
        var service = new AuthService(users, new FakePasswordHasher(), new FakeTokenGenerator());

        var result = await service.RegisterAsync(new RegisterRequest
        {
            UserName = "alice",
            Email = "alice@example.test",
            Password = new string('x', 129)
        });

        Assert.False(result.Success);
        Assert.Empty(users.Users);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<UserAccount> Users { get; } = [];

        public Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(user => user.UserName == userName));

        public Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Any(user => user.UserName == userName));

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Any(user => user.Email == email));

        public Task AddAsync(UserAccount user, CancellationToken cancellationToken)
        {
            user.Id = Users.Count + 1;
            Users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string passwordHash, string password) => passwordHash == HashPassword(password);
    }

    private sealed class FakeTokenGenerator : IJwtTokenGenerator
    {
        public GeneratedToken GenerateToken(UserAccount user) => new("test-token", DateTimeOffset.UtcNow.AddMinutes(15));
    }
}
