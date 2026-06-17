namespace MicCheck.Api.Features;

public record struct FeatureUsageBucketKey(int EnvironmentId, int FeatureId, string FeatureName, DateOnly UsageDate);
