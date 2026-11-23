using InvestorChat.Domain.Entities;

namespace InvestorChat.Application.Security;

public interface IJwtTokenGenerator
{
    GeneratedToken GenerateToken(UserAccount user);
}
