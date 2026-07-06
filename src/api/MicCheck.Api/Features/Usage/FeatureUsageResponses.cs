namespace MicCheck.Api.Features.Usage;

public record TopFeatureUsage(int FeatureId, string FeatureName, long Count);

public record DailyUsage(DateOnly Date, long TotalCount, IReadOnlyList<TopFeatureUsage> Features);

public record DashboardUsageResponse(IReadOnlyList<TopFeatureUsage> TopFeaturesLastDay, IReadOnlyList<DailyUsage> DailyUsage);
