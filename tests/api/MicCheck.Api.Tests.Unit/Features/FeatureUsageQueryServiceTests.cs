using MicCheck.Api.Data;
using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureUsageQueryServiceTests
{
    private MicCheckDbContext _db = null!;
    private FeatureUsageQueryService _service = null!;
    private const int EnvironmentId = 1;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new MicCheckDbContext(options);
        _service = new FeatureUsageQueryService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void SeedUsage(int environmentId, int featureId, string featureName, DateOnly date, long count)
    {
        _db.FeatureUsageDaily.Add(new FeatureUsageDaily
        {
            EnvironmentId = environmentId,
            FeatureId = featureId,
            FeatureName = featureName,
            UsageDate = date,
            Count = count,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    [Test]
    public async Task WhenNoUsageData_ThenEmptyCollectionsAreReturned()
    {
        var result = await _service.GetDashboardUsageAsync(EnvironmentId, days: 14);

        Assert.That(result.TopFeaturesLastDay, Is.Empty);
        Assert.That(result.DailyUsage, Is.Empty);
    }

    [Test]
    public async Task WhenMultipleFeaturesEvaluatedToday_ThenTopFeaturesLastDayOrdersByCountDescending()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedUsage(EnvironmentId, featureId: 1, "feature_a", today, count: 50);
        SeedUsage(EnvironmentId, featureId: 2, "feature_b", today, count: 200);
        SeedUsage(EnvironmentId, featureId: 3, "feature_c", today, count: 10);

        var result = await _service.GetDashboardUsageAsync(EnvironmentId, days: 1);

        Assert.That(result.TopFeaturesLastDay, Has.Count.EqualTo(3));
        Assert.That(result.TopFeaturesLastDay[0].FeatureName, Is.EqualTo("feature_b"));
        Assert.That(result.TopFeaturesLastDay[0].Count, Is.EqualTo(200));
        Assert.That(result.TopFeaturesLastDay[1].FeatureName, Is.EqualTo("feature_a"));
        Assert.That(result.TopFeaturesLastDay[2].FeatureName, Is.EqualTo("feature_c"));
    }

    [Test]
    public async Task WhenMoreThanTenFeatures_ThenTopFeaturesLastDayLimitsToTen()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 1; i <= 15; i++)
            SeedUsage(EnvironmentId, featureId: i, $"feature_{i}", today, count: i * 10L);

        var result = await _service.GetDashboardUsageAsync(EnvironmentId, days: 1);

        Assert.That(result.TopFeaturesLastDay, Has.Count.EqualTo(10));
    }

    [Test]
    public async Task WhenUsageSpansMultipleDays_ThenDailyUsageGroupsByDateAscending()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedUsage(EnvironmentId, featureId: 1, "flag", today.AddDays(-2), count: 30);
        SeedUsage(EnvironmentId, featureId: 1, "flag", today.AddDays(-1), count: 50);
        SeedUsage(EnvironmentId, featureId: 1, "flag", today, count: 20);

        var result = await _service.GetDashboardUsageAsync(EnvironmentId, days: 7);

        Assert.That(result.DailyUsage, Has.Count.EqualTo(3));
        Assert.That(result.DailyUsage[0].Date, Is.EqualTo(today.AddDays(-2)));
        Assert.That(result.DailyUsage[0].TotalCount, Is.EqualTo(30));
        Assert.That(result.DailyUsage[1].TotalCount, Is.EqualTo(50));
        Assert.That(result.DailyUsage[2].TotalCount, Is.EqualTo(20));
    }

    [Test]
    public async Task WhenHoveringADay_ThenDailyUsageIncludesPerFeatureBreakdownOrderedByCount()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedUsage(EnvironmentId, featureId: 1, "feature_a", today, count: 10);
        SeedUsage(EnvironmentId, featureId: 2, "feature_b", today, count: 40);

        var result = await _service.GetDashboardUsageAsync(EnvironmentId, days: 1);

        var day = result.DailyUsage.Single();
        Assert.That(day.TotalCount, Is.EqualTo(50));
        Assert.That(day.Features, Has.Count.EqualTo(2));
        Assert.That(day.Features[0].FeatureName, Is.EqualTo("feature_b"));
        Assert.That(day.Features[0].Count, Is.EqualTo(40));
        Assert.That(day.Features[1].FeatureName, Is.EqualTo("feature_a"));
    }

    [Test]
    public async Task WhenUsageExistsOutsideDaysWindow_ThenOldDataIsExcluded()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedUsage(EnvironmentId, featureId: 1, "flag", today.AddDays(-30), count: 999);
        SeedUsage(EnvironmentId, featureId: 1, "flag", today.AddDays(-3), count: 5);

        var result = await _service.GetDashboardUsageAsync(EnvironmentId, days: 7);

        Assert.That(result.DailyUsage, Has.Count.EqualTo(1));
        Assert.That(result.DailyUsage[0].TotalCount, Is.EqualTo(5));
    }

    [Test]
    public async Task WhenOtherEnvironmentHasUsage_ThenItIsNotIncludedInResults()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedUsage(environmentId: 99, featureId: 1, "flag", today, count: 100);

        var result = await _service.GetDashboardUsageAsync(EnvironmentId, days: 7);

        Assert.That(result.TopFeaturesLastDay, Is.Empty);
        Assert.That(result.DailyUsage, Is.Empty);
    }
}
