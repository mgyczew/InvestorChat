using InvestorChat.Application.Persistence;
using InvestorChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestorChat.Infrastructure.Persistence;

internal sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.UserName == userName, cancellationToken);

    public Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.UserName == userName, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public async Task AddAsync(UserAccount user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
