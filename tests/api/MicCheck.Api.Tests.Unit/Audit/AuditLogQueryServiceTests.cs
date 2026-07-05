using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Users;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Audit;

[TestFixture]
public class AuditLogQueryServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AuditLog> _auditLogs = null!;
    private List<User> _users = null!;
    private AuditLogQueryService _service = null!;

    private const int OrganizationId = 1;
    private const int ProjectId = 1;
    private const int EnvironmentId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _auditLogs = [];
        _db.SetupDbSet(c => c.AuditLogs, _auditLogs);
        _users = [];
        _db.SetupDbSet(c => c.Users, _users);

        _service = new AuditLogQueryService(_db.Object);
    }

    private AuditLog AddLog(string resourceType = "Feature", string action = "created", int? projectId = ProjectId,
        int? environmentId = EnvironmentId, int? actorUserId = null, DateTimeOffset? createdAt = null)
    {
        var log = new AuditLog
        {
            Id = _auditLogs.Count + 1,
            ResourceType = resourceType,
            ResourceId = "1",
            Action = action,
            OrganizationId = OrganizationId,
            ProjectId = projectId,
            EnvironmentId = environmentId,
            ActorUserId = actorUserId,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };
        _auditLogs.Add(log);
        return log;
    }

    [Test]
    public async Task WhenListingByOrganization_ThenOnlyLogsForThatOrganizationAreReturned()
    {
        AddLog();
        _auditLogs.Add(new AuditLog { Id = 2, ResourceType = "Feature", ResourceId = "1", Action = "created", OrganizationId = 999, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter());

        Assert.That(result.Total, Is.EqualTo(1));
        Assert.That(result.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenListingByProject_ThenOnlyLogsForThatProjectAreReturned()
    {
        AddLog(projectId: ProjectId);
        AddLog(projectId: 999);

        var result = await _service.ListByProjectAsync(ProjectId, new AuditLogFilter());

        Assert.That(result.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenListingByEnvironment_ThenOnlyLogsForThatEnvironmentAreReturned()
    {
        AddLog(environmentId: EnvironmentId);
        AddLog(environmentId: 999);

        var result = await _service.ListByEnvironmentAsync(EnvironmentId, new AuditLogFilter());

        Assert.That(result.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenFilteringByFromDate_ThenOnlyLogsOnOrAfterAreReturned()
    {
        AddLog(createdAt: DateTimeOffset.UtcNow.AddDays(-10));
        AddLog(createdAt: DateTimeOffset.UtcNow);

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(From: DateTimeOffset.UtcNow.AddDays(-1)));

        Assert.That(result.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenFilteringByToDate_ThenOnlyLogsOnOrBeforeAreReturned()
    {
        AddLog(createdAt: DateTimeOffset.UtcNow.AddDays(-10));
        AddLog(createdAt: DateTimeOffset.UtcNow);

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(To: DateTimeOffset.UtcNow.AddDays(-1)));

        Assert.That(result.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenFilteringByResourceType_ThenOnlyMatchingLogsAreReturned()
    {
        AddLog(resourceType: "Feature");
        AddLog(resourceType: "Segment");

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(ResourceType: "Segment"));

        Assert.That(result.Total, Is.EqualTo(1));
        Assert.That(result.Items[0].ResourceType, Is.EqualTo("Segment"));
    }

    [Test]
    public async Task WhenFilteringByAction_ThenOnlyMatchingLogsAreReturned()
    {
        AddLog(action: "created");
        AddLog(action: "deleted");

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(Action: "deleted"));

        Assert.That(result.Total, Is.EqualTo(1));
        Assert.That(result.Items[0].Action, Is.EqualTo("deleted"));
    }

    [Test]
    public async Task WhenFilteringByProjectIdOnAnOrganizationQuery_ThenOnlyMatchingLogsAreReturned()
    {
        AddLog(projectId: ProjectId);
        AddLog(projectId: 999);

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(ProjectId: ProjectId));

        Assert.That(result.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenFilteringByEnvironmentIdOnAnOrganizationQuery_ThenOnlyMatchingLogsAreReturned()
    {
        AddLog(environmentId: EnvironmentId);
        AddLog(environmentId: 999);

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(EnvironmentId: EnvironmentId));

        Assert.That(result.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenFilteringByActorUserId_ThenOnlyMatchingLogsAreReturned()
    {
        AddLog(actorUserId: 1);
        AddLog(actorUserId: 2);

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(ActorUserId: 1));

        Assert.That(result.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenPageSizeExceedsMaximum_ThenItIsClampedTo100()
    {
        for (var i = 0; i < 150; i++)
            AddLog();

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(PageSize: 1000));

        Assert.That(result.Items, Has.Count.EqualTo(100));
    }

    [Test]
    public async Task WhenPageSizeIsBelowMinimum_ThenItIsClampedTo1()
    {
        AddLog();
        AddLog();

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter(PageSize: 0));

        Assert.That(result.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenResultsAreOrderedByCreationDate_ThenNewestAppearsFirst()
    {
        AddLog(createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        var newest = AddLog(createdAt: DateTimeOffset.UtcNow);

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter());

        Assert.That(result.Items[0].Id, Is.EqualTo(newest.Id));
    }

    [Test]
    public async Task WhenALogHasAnActorUser_ThenTheActorUserNameIsPopulated()
    {
        _users.Add(new User { Id = 7, Email = "alice@example.com", PasswordHash = "hash", FirstName = "Alice", LastName = "Smith", CreatedAt = DateTimeOffset.UtcNow });
        AddLog(actorUserId: 7);

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter());

        Assert.That(result.Items[0].ActorUserName, Is.EqualTo("Alice Smith"));
    }

    [Test]
    public async Task WhenALogHasNoActorUser_ThenTheActorUserNameIsNull()
    {
        AddLog();

        var result = await _service.ListByOrganizationAsync(OrganizationId, new AuditLogFilter());

        Assert.That(result.Items[0].ActorUserName, Is.Null);
    }
}
