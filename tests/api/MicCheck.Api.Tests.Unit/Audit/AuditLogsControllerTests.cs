using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Audit;

[TestFixture]
public class AuditLogsControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Organization> _organizations = null!;
    private List<Project> _projects = null!;
    private List<AppEnvironment> _environments = null!;
    private List<AuditLog> _auditLogs = null!;
    private AuditLogsController _controller = null!;

    private const int OrganizationId = 1;
    private const int ProjectId = 1;
    private const int EnvironmentId = 1;
    private const string ApiKey = "env-key";

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _organizations = [new Organization { Id = OrganizationId, Name = "Org", CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Organizations, _organizations);

        _projects = [new Project { Id = ProjectId, Name = "Project", OrganizationId = OrganizationId, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Projects, _projects);

        _environments = [new AppEnvironment { Id = EnvironmentId, Name = "Production", ApiKey = ApiKey, ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Environments, _environments);

        _auditLogs = [];
        _db.SetupDbSet(c => c.AuditLogs, _auditLogs);
        _db.SetupDbSet(c => c.Users, []);

        var auditServiceMock = new Mock<AuditService>(_db.Object, null!, new MicCheck.Api.Webhooks.WebhookQueue());
        var auditService = auditServiceMock.Object;

        _controller = new AuditLogsController(
            new AuditLogQueryService(_db.Object),
            new OrganizationService(_db.Object, auditService),
            new ProjectService(_db.Object, auditService),
            new EnvironmentService(_db.Object, auditService));
    }

    [Test]
    public async Task WhenListingByOrganizationThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListByOrganization(999, new AuditLogFilter(), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingByOrganizationThatExists_ThenMatchingLogsAreReturned()
    {
        _auditLogs.Add(new AuditLog { Id = 1, ResourceType = "Feature", ResourceId = "1", Action = "created", OrganizationId = OrganizationId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListByOrganization(OrganizationId, new AuditLogFilter(), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as PaginatedResponse<AuditLogResponse>;
        Assert.That(body!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenListingByProjectThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListByProject(999, new AuditLogFilter(), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingByProjectThatExists_ThenMatchingLogsAreReturned()
    {
        _auditLogs.Add(new AuditLog { Id = 1, ResourceType = "Feature", ResourceId = "1", Action = "created", OrganizationId = OrganizationId, ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListByProject(ProjectId, new AuditLogFilter(), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as PaginatedResponse<AuditLogResponse>;
        Assert.That(body!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenListingByEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListByEnvironment("missing-key", new AuditLogFilter(), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingByEnvironmentThatExists_ThenMatchingLogsAreReturned()
    {
        _auditLogs.Add(new AuditLog { Id = 1, ResourceType = "Feature", ResourceId = "1", Action = "created", OrganizationId = OrganizationId, EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListByEnvironment(ApiKey, new AuditLogFilter(), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as PaginatedResponse<AuditLogResponse>;
        Assert.That(body!.Count, Is.EqualTo(1));
    }
}
