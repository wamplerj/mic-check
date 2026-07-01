namespace MicCheck.Api.Common.Security.ApiKeys;

public class ApiKey
{
    public int Id { get; init; }
    public required string Key { get; set; }
    public required string Prefix { get; set; }
    public required string Name { get; set; }
    public int OrganizationId { get; init; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
