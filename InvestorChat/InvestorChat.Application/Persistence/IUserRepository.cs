using InvestorChat.Domain.Entities;

namespace InvestorChat.Application.Persistence;

public interface IUserRepository
{
    Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken);
    Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(UserAccount user, CancellationToken cancellationToken);
}
