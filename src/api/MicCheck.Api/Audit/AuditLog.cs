namespace MicCheck.Api.Audit;

public record AuditLog
{
    public int Id { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public required string Action { get; init; }
    public string? Changes { get; init; }
    public int OrganizationId { get; init; }
    public int? ProjectId { get; init; }
    public int? EnvironmentId { get; init; }
    public int? ActorUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
