namespace MicCheck.Api.Common.Security.ApiKeys;

public record ApiKeyResponse(int Id, string Name, string Prefix, bool IsActive, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);
