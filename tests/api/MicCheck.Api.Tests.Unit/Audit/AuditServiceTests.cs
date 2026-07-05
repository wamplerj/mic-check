using System.Text.Json;
using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Webhooks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Audit;

[TestFixture]
public class AuditServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AuditLog> _auditLogs = null!;
    private AuditService _service = null!;
    private WebhookQueue _queue = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _auditLogs = [];
        _db.SetupDbSet(c => c.AuditLogs, _auditLogs);
        _queue = new WebhookQueue();

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        _service = new AuditService(_db.Object, httpContextAccessor.Object, _queue);
    }

    [Test]
    public async Task WhenRecordingWithNoBefore_ThenChangesIsNull()
    {
        await _service.RecordAsync("Feature", "1", "created", organizationId: 1);

        var log = _auditLogs.First();
        Assert.That(log.Changes, Is.Null);
    }

    [Test]
    public async Task WhenRecordingWithAfterOnly_ThenChangesContainsAfterJson()
    {
        var after = new { Name = "dark_mode", Enabled = true };
        await _service.RecordAsync("Feature", "1", "created", organizationId: 1, after: after);

        var log = _auditLogs.First();
        Assert.That(log.Changes, Is.Not.Null);

        var changes = JsonDocument.Parse(log.Changes!).RootElement;
        Assert.That(changes.GetProperty("after").GetProperty("name").GetString(), Is.EqualTo("dark_mode"));
        Assert.That(changes.TryGetProperty("before", out var beforeProp), Is.True);
        Assert.That(beforeProp.ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task WhenRecordingWithBeforeAndAfter_ThenChangesCapturesBothStates()
    {
        var before = new { Enabled = false, Value = "old" };
        var after = new { Enabled = true, Value = "new" };

        await _service.RecordAsync("FeatureState", "42", "updated", organizationId: 1,
            before: before, after: after);

        var log = _auditLogs.First();
        var changes = JsonDocument.Parse(log.Changes!).RootElement;

        Assert.That(changes.GetProperty("before").GetProperty("enabled").GetBoolean(), Is.False);
        Assert.That(changes.GetProperty("after").GetProperty("enabled").GetBoolean(), Is.True);
        Assert.That(changes.GetProperty("before").GetProperty("value").GetString(), Is.EqualTo("old"));
        Assert.That(changes.GetProperty("after").GetProperty("value").GetString(), Is.EqualTo("new"));
    }

    [Test]
    public async Task WhenRecording_ThenMetadataIsPersistedCorrectly()
    {
        await _service.RecordAsync(
            "Segment", "5", "deleted",
            organizationId: 10, projectId: 20, environmentId: 30);

        var log = _auditLogs.First();
        Assert.That(log.ResourceType, Is.EqualTo("Segment"));
        Assert.That(log.ResourceId, Is.EqualTo("5"));
        Assert.That(log.Action, Is.EqualTo("deleted"));
        Assert.That(log.OrganizationId, Is.EqualTo(10));
        Assert.That(log.ProjectId, Is.EqualTo(20));
        Assert.That(log.EnvironmentId, Is.EqualTo(30));
        Assert.That(log.ActorUserId, Is.Null);
    }

    [Test]
    public async Task WhenLoggingWithNoChanges_ThenChangesRemainsNull()
    {
        await _service.LogAsync("Feature", "1", "created", organizationId: 1);

        var log = _auditLogs.First();
        Assert.That(log.Changes, Is.Null);
    }

    [Test]
    public async Task WhenLoggingWithChanges_ThenChangesAndMetadataArePersisted()
    {
        await _service.LogAsync("Feature", "2", "updated", organizationId: 5, projectId: 6, environmentId: 7, changes: "raw-json");

        var log = _auditLogs.First();
        Assert.That(log.ResourceType, Is.EqualTo("Feature"));
        Assert.That(log.ResourceId, Is.EqualTo("2"));
        Assert.That(log.Action, Is.EqualTo("updated"));
        Assert.That(log.Changes, Is.EqualTo("raw-json"));
        Assert.That(log.OrganizationId, Is.EqualTo(5));
        Assert.That(log.ProjectId, Is.EqualTo(6));
        Assert.That(log.EnvironmentId, Is.EqualTo(7));
    }

    [Test]
    public async Task WhenLogging_ThenAuditLogCreatedEventIsEnqueued()
    {
        await _service.LogAsync("Feature", "1", "created", organizationId: 1);

        var events = new List<WebhookEvent>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try
        {
            await foreach (var e in _queue.ReadAllAsync(cts.Token))
            {
                events.Add(e);
                break;
            }
        }
        catch (OperationCanceledException) { }

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].EventType, Is.EqualTo(WebhookEventTypes.AuditLogCreated));
    }

    [Test]
    public async Task WhenTheHttpContextHasAnAuthenticatedUser_ThenTheActorUserIdIsResolvedFromTheClaim()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "42")], "TestAuth");
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(identity)
        };
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var service = new AuditService(_db.Object, httpContextAccessor.Object, _queue);

        await service.RecordAsync("Feature", "1", "created", organizationId: 1);

        Assert.That(_auditLogs.First().ActorUserId, Is.EqualTo(42));
    }

    [Test]
    public async Task WhenTheHttpContextHasNoNameIdentifierClaim_ThenTheActorUserIdIsNull()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())
        };
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var service = new AuditService(_db.Object, httpContextAccessor.Object, _queue);

        await service.RecordAsync("Feature", "1", "created", organizationId: 1);

        Assert.That(_auditLogs.First().ActorUserId, Is.Null);
    }

    [Test]
    public async Task WhenRecording_ThenAuditLogCreatedEventIsEnqueued()
    {
        await _service.RecordAsync("Feature", "1", "created", organizationId: 1);

        var events = new List<WebhookEvent>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try
        {
            await foreach (var e in _queue.ReadAllAsync(cts.Token))
            {
                events.Add(e);
                break;
            }
        }
        catch (OperationCanceledException) { }

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].EventType, Is.EqualTo(WebhookEventTypes.AuditLogCreated));
    }
}
