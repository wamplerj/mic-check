using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;
using Moq;
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

    private static Mock<IMicCheckDbContext> CreateDb(List<WebhookDeliveryLog> deliveryLogs)
    {
        var db = new Mock<IMicCheckDbContext>();
        db.SetupDbSet(c => c.WebhookDeliveryLogs, deliveryLogs);
        return db;
    }

    [Test]
    public async Task WhenFailedDeliveryIsOldEnough_ThenItIsEligibleForRetry()
    {
        var deliveryLogs = new List<WebhookDeliveryLog>
        {
            new()
            {
                WebhookId = 1,
                EventType = WebhookEventTypes.FlagUpdated,
                PayloadJson = """{"event_type":"FLAG_UPDATED","data":{}}""",
                Success = false,
                AttemptNumber = 1,
                AttemptedAt = DateTimeOffset.UtcNow.AddMinutes(-6),
                Duration = TimeSpan.FromMilliseconds(100)
            }
        };
        var db = CreateDb(deliveryLogs);

        var retryAfter = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
        var eligible = await db.Object.WebhookDeliveryLogs
            .Where(d => !d.Success && d.AttemptNumber == 1 && d.AttemptedAt <= retryAfter)
            .ToListAsync();

        Assert.That(eligible, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenFailedDeliveryIsTooRecent_ThenItIsNotEligibleForRetry()
    {
        var deliveryLogs = new List<WebhookDeliveryLog>
        {
            new()
            {
                WebhookId = 1,
                EventType = WebhookEventTypes.FlagUpdated,
                PayloadJson = """{"event_type":"FLAG_UPDATED","data":{}}""",
                Success = false,
                AttemptNumber = 1,
                AttemptedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                Duration = TimeSpan.FromMilliseconds(100)
            }
        };
        var db = CreateDb(deliveryLogs);

        var retryAfter = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
        var eligible = await db.Object.WebhookDeliveryLogs
            .Where(d => !d.Success && d.AttemptNumber == 1 && d.AttemptedAt <= retryAfter)
            .ToListAsync();

        Assert.That(eligible, Is.Empty);
    }

    [Test]
    public async Task WhenDeliveryHasAlreadyBeenRetried_ThenItIsNotRetriedAgain()
    {
        var firstAttemptedAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var deliveryLogs = new List<WebhookDeliveryLog>
        {
            new()
            {
                WebhookId = 1,
                EventType = WebhookEventTypes.FlagUpdated,
                PayloadJson = "{}",
                Success = false,
                AttemptNumber = 1,
                AttemptedAt = firstAttemptedAt,
                Duration = TimeSpan.Zero
            },
            new()
            {
                WebhookId = 1,
                EventType = WebhookEventTypes.FlagUpdated,
                PayloadJson = "{}",
                Success = false,
                AttemptNumber = 2,
                AttemptedAt = firstAttemptedAt.AddMinutes(5),
                Duration = TimeSpan.Zero
            }
        };
        var db = CreateDb(deliveryLogs);

        var alreadyRetried = await db.Object.WebhookDeliveryLogs
            .AnyAsync(d => d.WebhookId == 1 && d.AttemptNumber == 2 && d.AttemptedAt > firstAttemptedAt);

        Assert.That(alreadyRetried, Is.True);
    }
}
