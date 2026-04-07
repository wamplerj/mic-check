namespace MicCheck.Api.Auth;

public interface ITokenService
{
    TokenResponse GenerateToken(string username);
}
