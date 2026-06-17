namespace MicCheck.Api.Features;

public class FeatureUsageDaily
{
    public int Id { get; init; }
    public int EnvironmentId { get; init; }
    public int FeatureId { get; init; }
    public required string FeatureName { get; set; }
    public DateOnly UsageDate { get; init; }
    public long Count { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
