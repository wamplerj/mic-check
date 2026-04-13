namespace MicCheck.Api.Authorization;

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
