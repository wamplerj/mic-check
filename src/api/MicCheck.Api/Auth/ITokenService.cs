using MicCheck.Api.Users;

namespace MicCheck.Api.Auth;

public interface ITokenService
{
    TokenResponse GenerateToken(User user);
}
