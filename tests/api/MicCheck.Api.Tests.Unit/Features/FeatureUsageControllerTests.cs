using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureUsageControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<FeatureUsageDaily> _usage = null!;
    private IMemoryCache _cache = null!;
    private FeatureUsageController _controller = null!;
    private const int EnvironmentId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _usage = [];
        _db.SetupDbSet(c => c.FeatureUsageDaily, _usage);
        _cache = new MemoryCache(new MemoryCacheOptions());

        _controller = new FeatureUsageController(new FeatureUsageQueryService(_db.Object), _cache);
    }

    [Test]
    public async Task WhenGettingDashboardUsageWithNoData_ThenEmptyResultsAreReturned()
    {
        var result = await _controller.GetDashboardUsage(EnvironmentId);

        var ok = result.Result as OkObjectResult;
        var response = (DashboardUsageResponse)ok!.Value!;
        Assert.That(response.TopFeaturesLastDay, Is.Empty);
    }

    [Test]
    public async Task WhenGettingDashboardUsageASecondTime_ThenTheCachedResultIsReturnedWithoutQueryingAgain()
    {
        var first = await _controller.GetDashboardUsage(EnvironmentId);
        _usage.Add(new FeatureUsageDaily
        {
            EnvironmentId = EnvironmentId,
            FeatureId = 1,
            FeatureName = "flag",
            UsageDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Count = 100,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var second = await _controller.GetDashboardUsage(EnvironmentId);

        var firstOk = (DashboardUsageResponse)((OkObjectResult)first.Result!).Value!;
        var secondOk = (DashboardUsageResponse)((OkObjectResult)second.Result!).Value!;
        Assert.That(secondOk, Is.SameAs(firstOk));
    }

    [Test]
    public async Task WhenGettingDashboardUsageForDifferentDayRanges_ThenEachRangeIsCachedSeparately()
    {
        var sevenDays = await _controller.GetDashboardUsage(EnvironmentId, days: 7);
        var fourteenDays = await _controller.GetDashboardUsage(EnvironmentId, days: 14);

        var sevenOk = (DashboardUsageResponse)((OkObjectResult)sevenDays.Result!).Value!;
        var fourteenOk = (DashboardUsageResponse)((OkObjectResult)fourteenDays.Result!).Value!;
        Assert.That(sevenOk, Is.Not.SameAs(fourteenOk));
    }

    [Test]
    public async Task WhenDaysExceedsTheMaximum_ThenItIsClampedTo90()
    {
        _usage.Add(new FeatureUsageDaily
        {
            EnvironmentId = EnvironmentId,
            FeatureId = 1,
            FeatureName = "flag",
            UsageDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Count = 5,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var clamped = await _controller.GetDashboardUsage(EnvironmentId, days: 1000);
        var explicitNinety = await _controller.GetDashboardUsage(EnvironmentId, days: 90);

        var clampedOk = (DashboardUsageResponse)((OkObjectResult)clamped.Result!).Value!;
        var explicitOk = (DashboardUsageResponse)((OkObjectResult)explicitNinety.Result!).Value!;
        Assert.That(clampedOk.DailyUsage.Count, Is.EqualTo(explicitOk.DailyUsage.Count));
    }
}
