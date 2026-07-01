namespace MicCheck.Api.Common.Security.ApiKeys;

public record CreateApiKeyRequest(string Name, DateTimeOffset? ExpiresAt);
