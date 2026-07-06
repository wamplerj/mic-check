using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Admin;

[TestFixture]
public class AdminApiIntegrationTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Project> _projects = null!;
    private List<Feature> _features = null!;
    private List<FeatureState> _featureStates = null!;
    private List<AppEnvironment> _environments = null!;
    private List<Segment> _segments = null!;
    private Mock<IAuditService> _auditService = null!;
    private int _organizationId;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _db.SetupDbSetWithGeneratedIds(c => c.Organizations, [
            new Organization { Name = "Test Org", CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _organizationId = 1;

        _projects = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Projects, _projects);
        _features = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Features, _features);
        _featureStates = [];
        _db.SetupDbSet(c => c.FeatureStates, _featureStates);
        _environments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Environments, _environments);
        _segments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Segments, _segments);
        _db.SetupDbSetWithGeneratedIds(c => c.SegmentRules, []);
        _db.SetupDbSetWithGeneratedIds(c => c.SegmentConditions, []);

        var webhookQueue = new WebhookQueue();
        _auditService = new Mock<IAuditService>();
        _auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Project AddProject(string name = "My Project")
    {
        var project = new Project
        {
            Name = name,
            OrganizationId = _organizationId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Object.Projects.Add(project);
        return project;
    }

    [Test]
    public async Task WhenCreatingAProject_ThenProjectIsReturned()
    {
        var controller = new ProjectsController(new ProjectService(_db.Object, _auditService.Object));

        var result = await controller.Create(new CreateProjectRequest("My Project", _organizationId), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task WhenCreatingAFeature_ThenFeatureIsReturned()
    {
        var project = AddProject();
        var controller = new FeaturesController(new FeatureService(_db.Object, _auditService.Object, new WebhookQueue()));

        var result = await controller.Create(project.Id,
            new CreateFeatureRequest("dark_mode", FeatureType.Standard, null, null), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task WhenCreatingAFeature_ThenFeatureStateIsAutoCreatedForEnvironments()
    {
        var project = AddProject();
        _db.Object.Environments.Add(new AppEnvironment
        {
            Name = "Production",
            ApiKey = "env-key-prod",
            ProjectId = project.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var controller = new FeaturesController(new FeatureService(_db.Object, _auditService.Object, new WebhookQueue()));

        await controller.Create(project.Id,
            new CreateFeatureRequest("flag_x", FeatureType.Standard, null, null), CancellationToken.None);

        var feature = _features.First(f => f.Name == "flag_x");
        var states = _featureStates.Where(fs => fs.FeatureId == feature.Id).ToList();

        Assert.That(states, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAnEnvironment_ThenFeatureStateIsAutoCreatedForExistingFeatures()
    {
        var project = AddProject();
        _db.Object.Features.Add(new Feature
        {
            Name = "existing_flag",
            ProjectId = project.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var controller = new EnvironmentsController(
            new EnvironmentService(_db.Object, _auditService.Object),
            new WebhookService(_db.Object));

        var result = await controller.Create(new CreateEnvironmentRequest("Staging", project.Id), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());

        var feature = _features.First(f => f.Name == "existing_flag");
        var env = _environments.First(e => e.ProjectId == project.Id);
        var states = _featureStates.Where(fs => fs.EnvironmentId == env.Id && fs.FeatureId == feature.Id).ToList();

        Assert.That(states, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingASegment_ThenSegmentIsReturned()
    {
        var project = AddProject();
        var controller = new SegmentsController(new SegmentService(_db.Object, _auditService.Object));

        var result = await controller.Create(project.Id, new CreateSegmentRequest(
            "Premium Users",
            [
                new CreateSegmentRuleRequest(
                    "All",
                    [new CreateSegmentConditionRequest("plan", "Equal", "premium")])
            ]), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
    }
}
