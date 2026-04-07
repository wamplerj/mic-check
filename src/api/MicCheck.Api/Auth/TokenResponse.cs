namespace MicCheck.Api.Auth;

public record TokenResponse(string Token, DateTime ExpiresAt);
