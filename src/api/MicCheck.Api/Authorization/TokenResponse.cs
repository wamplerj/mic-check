namespace MicCheck.Api.Authorization;

public record TokenResponse(string Token, DateTime ExpiresAt);
