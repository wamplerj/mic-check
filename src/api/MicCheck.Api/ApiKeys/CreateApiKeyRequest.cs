namespace MicCheck.Api.ApiKeys;

public record CreateApiKeyRequest(string Name, DateTimeOffset? ExpiresAt);
