namespace MicCheck.Api.Common.Security.ApiKeys;

public class ApiKey
{
    public int Id { get; init; }
    public required string Key { get; init; }
    public required string Prefix { get; init; }
    public required string Name { get; init; }
    public int OrganizationId { get; init; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
