namespace MicCheck.Api.Features.Usage;

public record struct FeatureUsageBucketKey(int EnvironmentId, int FeatureId, string FeatureName, DateOnly UsageDate);
