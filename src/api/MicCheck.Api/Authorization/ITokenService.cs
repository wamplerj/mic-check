using MicCheck.Api.Users;

namespace MicCheck.Api.Authorization;

public interface ITokenService
{
    TokenResponse GenerateToken(User user);
}
