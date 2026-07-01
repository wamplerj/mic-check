namespace MicCheck.Api.Common.Security.Authorization;

public record TokenResponse(string Token, DateTime ExpiresAt);
