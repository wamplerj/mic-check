using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureUsageFlushBackgroundService(
    FeatureUsageMetrics metrics,
    IServiceScopeFactory scopeFactory,
    ILogger<FeatureUsageFlushBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushAsync(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(CancellationToken.None);
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        var deltas = metrics.DrainAccumulated();
        if (deltas.Count == 0)
            return;

        var updatedAt = DateTimeOffset.UtcNow;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();

            foreach (var (key, count) in deltas)
            {
                await db.Database.ExecuteSqlAsync(
                    $"""
                    INSERT INTO "FeatureUsageDaily" ("EnvironmentId", "FeatureId", "FeatureName", "UsageDate", "Count", "UpdatedAt")
                    VALUES ({key.EnvironmentId}, {key.FeatureId}, {key.FeatureName}, {key.UsageDate}, {count}, {updatedAt})
                    ON CONFLICT ("EnvironmentId", "FeatureId", "UsageDate")
                    DO UPDATE SET "Count" = "FeatureUsageDaily"."Count" + EXCLUDED."Count",
                                  "FeatureName" = EXCLUDED."FeatureName",
                                  "UpdatedAt" = EXCLUDED."UpdatedAt"
                    """,
                    ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to flush feature usage data ({BucketCount} buckets)", deltas.Count);
        }
    }
}
