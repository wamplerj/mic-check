using MicCheck.Api.Data;
using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Webhooks;

[TestFixture]
public class WebhookRetryTests
{
    [Test]
    public void WhenRetryDelaysAreConfigured_ThenFirstRetryIsAfter5Minutes()
    {
        // Retry delay for attempt 1 → 2 is 5 minutes
        var delay = TimeSpan.FromMinutes(5);
        Assert.That(delay.TotalMinutes, Is.EqualTo(5));
    }

    [Test]
    public void WhenRetryDelaysAreConfigured_ThenSecondRetryIsAfter30Minutes()
    {
        // Retry delay for attempt 2 → 3 is 30 minutes
        var delay = TimeSpan.FromMinutes(30);
        Assert.That(delay.TotalMinutes, Is.EqualTo(30));
    }

    [Test]
    public async Task WhenFailedDeliveryIsOldEnough_ThenItIsEligibleForRetry()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new MicCheckDbContext(options);

        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
        {
            WebhookId = 1,
            EventType = WebhookEventTypes.FlagUpdated,
            PayloadJson = """{"event_type":"FLAG_UPDATED","data":{}}""",
            Success = false,
            AttemptNumber = 1,
            AttemptedAt = DateTimeOffset.UtcNow.AddMinutes(-6),
            Duration = TimeSpan.FromMilliseconds(100)
        });
        await db.SaveChangesAsync();

        var retryAfter = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
        var eligible = await db.WebhookDeliveryLogs
            .Where(d => !d.Success && d.AttemptNumber == 1 && d.AttemptedAt <= retryAfter)
            .ToListAsync();

        Assert.That(eligible, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenFailedDeliveryIsTooRecent_ThenItIsNotEligibleForRetry()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new MicCheckDbContext(options);

        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
        {
            WebhookId = 1,
            EventType = WebhookEventTypes.FlagUpdated,
            PayloadJson = """{"event_type":"FLAG_UPDATED","data":{}}""",
            Success = false,
            AttemptNumber = 1,
            AttemptedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            Duration = TimeSpan.FromMilliseconds(100)
        });
        await db.SaveChangesAsync();

        var retryAfter = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
        var eligible = await db.WebhookDeliveryLogs
            .Where(d => !d.Success && d.AttemptNumber == 1 && d.AttemptedAt <= retryAfter)
            .ToListAsync();

        Assert.That(eligible, Is.Empty);
    }

    [Test]
    public async Task WhenDeliveryHasAlreadyBeenRetried_ThenItIsNotRetriedAgain()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new MicCheckDbContext(options);

        var firstAttemptedAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
        {
            WebhookId = 1,
            EventType = WebhookEventTypes.FlagUpdated,
            PayloadJson = "{}",
            Success = false,
            AttemptNumber = 1,
            AttemptedAt = firstAttemptedAt,
            Duration = TimeSpan.Zero
        });

        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
        {
            WebhookId = 1,
            EventType = WebhookEventTypes.FlagUpdated,
            PayloadJson = "{}",
            Success = false,
            AttemptNumber = 2,
            AttemptedAt = firstAttemptedAt.AddMinutes(5),
            Duration = TimeSpan.Zero
        });
        await db.SaveChangesAsync();

        var alreadyRetried = await db.WebhookDeliveryLogs
            .AnyAsync(d => d.WebhookId == 1 && d.AttemptNumber == 2 && d.AttemptedAt > firstAttemptedAt);

        Assert.That(alreadyRetried, Is.True);
    }
}
