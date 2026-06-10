namespace MicCheck.Api.Common.Security.ApiKeys;

public record CreateApiKeyResponse(
    int Id,
    string Name,
    string Key,
    string Prefix,
    DateTimeOffset? ExpiresAt);
