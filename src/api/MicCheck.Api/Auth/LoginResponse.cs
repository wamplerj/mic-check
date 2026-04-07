namespace MicCheck.Api.Auth;

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
