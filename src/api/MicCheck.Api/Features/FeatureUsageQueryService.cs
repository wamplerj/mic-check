using MicCheck.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Features;

public class FeatureUsageQueryService(IMicCheckDbContext db)
{
    public async Task<DashboardUsageResponse> GetDashboardUsageAsync(int environmentId, int days, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowStart = today.AddDays(-days);
        var yesterday = today.AddDays(-1);

        var rows = await db.FeatureUsageDaily
            .Where(u => u.EnvironmentId == environmentId && u.UsageDate >= windowStart)
            .ToListAsync(ct);

        var topFeaturesLastDay = rows
            .Where(u => u.UsageDate >= yesterday)
            .GroupBy(u => new { u.FeatureId, u.FeatureName })
            .Select(g => new TopFeatureUsage(g.Key.FeatureId, g.Key.FeatureName, g.Sum(u => u.Count)))
            .OrderByDescending(t => t.Count)
            .Take(10)
            .ToList();

        var dailyUsage = rows
            .GroupBy(u => u.UsageDate)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var features = g
                    .GroupBy(u => new { u.FeatureId, u.FeatureName })
                    .Select(fg => new TopFeatureUsage(fg.Key.FeatureId, fg.Key.FeatureName, fg.Sum(u => u.Count)))
                    .OrderByDescending(f => f.Count)
                    .ToList();
                return new DailyUsage(g.Key, features.Sum(f => f.Count), features);
            })
            .ToList();

        return new DashboardUsageResponse(topFeaturesLastDay, dailyUsage);
    }
}
