namespace MicCheck.Api.Features.Usage;

public record FeatureUsageDaily
{
    public int Id { get; init; }
    public int EnvironmentId { get; init; }
    public int FeatureId { get; init; }
    public required string FeatureName { get; init; }
    public DateOnly UsageDate { get; init; }
    public long Count { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
