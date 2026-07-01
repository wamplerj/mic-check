namespace MicCheck.Api.Features;

public class FeatureState
{
    public int Id { get; init; }
    public int FeatureId { get; init; }
    public int EnvironmentId { get; init; }
    public int? IdentityId { get; set; }
    public bool Enabled { get; set; }
    public string? Value { get; set; }
    public int? FeatureSegmentId { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int Version { get; set; }
}
