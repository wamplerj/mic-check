using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using MicCheck.Api.Identities;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class EnvironmentsControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AppEnvironment> _environments = null!;
    private List<Project> _projects = null!;
    private List<Feature> _features = null!;
    private List<FeatureState> _featureStates = null!;
    private List<Identity> _identities = null!;
    private List<IdentityTrait> _identityTraits = null!;
    private List<Segment> _segments = null!;
    private List<FeatureSegment> _featureSegments = null!;
    private List<Webhook> _webhooks = null!;
    private List<WebhookDeliveryLog> _webhookDeliveryLogs = null!;
    private List<AuditLog> _auditLogs = null!;

    private EnvironmentsController _controller = null!;
    private AuditLogQueryService _auditLogQueryService = null!;
    private AdminIdentityService _adminIdentityService = null!;
    private FeatureStateService _featureStateService = null!;
    private FeatureSegmentService _featureSegmentService = null!;
    private SegmentService _segmentService = null!;
    private SegmentEvaluator _segmentEvaluator = null!;

    private const int OrganizationId = 1;
    private const int ProjectId = 1;
    private const int EnvironmentId = 1;
    private AppEnvironment _environment = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _db.SetupDbSet(c => c.Organizations, [new Organization { Id = OrganizationId, Name = "Org", CreatedAt = DateTimeOffset.UtcNow }]);
        _projects = [new Project { Id = ProjectId, Name = "Project", OrganizationId = OrganizationId, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Projects, _projects);

        _environment = new AppEnvironment { Id = EnvironmentId, Name = "Production", ApiKey = "env-key", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _environments = [_environment];
        var environmentsSet = MockDbSetFactory.Create(_environments);
        environmentsSet.Setup(m => m.Add(It.IsAny<AppEnvironment>())).Callback<AppEnvironment>(env =>
        {
            typeof(AppEnvironment).GetProperty(nameof(AppEnvironment.Id))!.SetValue(env, _environments.Count + 1);
            _environments.Add(env);
        });
        environmentsSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken _) => _environments.FirstOrDefault(e => e.Id == (int)keys[0]));
        _db.Setup(c => c.Environments).Returns(environmentsSet.Object);

        _features = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Features, _features);

        _featureStates = [];
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureStates, _featureStates);

        _identities = [];
        var identitiesSet = MockDbSetFactory.Create(_identities);
        identitiesSet.Setup(m => m.Add(It.IsAny<Identity>())).Callback<Identity>(i =>
        {
            typeof(Identity).GetProperty(nameof(Identity.Id))!.SetValue(i, _identities.Count + 1);
            _identities.Add(i);
        });
        _db.Setup(c => c.Identities).Returns(identitiesSet.Object);

        _identityTraits = [];
        var traitsSet = MockDbSetFactory.Create(_identityTraits);
        traitsSet.Setup(m => m.Add(It.IsAny<IdentityTrait>())).Callback<IdentityTrait>(trait =>
        {
            typeof(IdentityTrait).GetProperty(nameof(IdentityTrait.Id))!.SetValue(trait, _identityTraits.Count + 1);
            _identityTraits.Add(trait);
            _identities.FirstOrDefault(i => i.Id == trait.IdentityId)?.Traits.Add(trait);
        });
        traitsSet.Setup(m => m.Remove(It.IsAny<IdentityTrait>())).Callback<IdentityTrait>(trait =>
        {
            _identityTraits.Remove(trait);
            _identities.FirstOrDefault(i => i.Id == trait.IdentityId)?.Traits.Remove(trait);
        });
        _db.Setup(c => c.IdentityTraits).Returns(traitsSet.Object);

        _segments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Segments, _segments);

        _featureSegments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureSegments, _featureSegments);

        _webhooks = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Webhooks, _webhooks);

        _webhookDeliveryLogs = [];
        _db.SetupDbSet(c => c.WebhookDeliveryLogs, _webhookDeliveryLogs);

        _auditLogs = [];
        _db.SetupDbSetWithGeneratedIds(c => c.AuditLogs, _auditLogs);
        _db.SetupDbSet(c => c.Users, []);

        var webhookQueue = new WebhookQueue();
        var auditServiceMock = new Mock<AuditService>(_db.Object, null!, webhookQueue);
        auditServiceMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditServiceMock.Setup(a => a.RecordAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var auditService = auditServiceMock.Object;

        _auditLogQueryService = new AuditLogQueryService(_db.Object);
        _adminIdentityService = new AdminIdentityService(_db.Object);
        _featureStateService = new FeatureStateService(_db.Object, webhookQueue, auditService);
        _featureSegmentService = new FeatureSegmentService(_db.Object);
        _segmentService = new SegmentService(_db.Object, auditService);
        _segmentEvaluator = new SegmentEvaluator();

        _controller = new EnvironmentsController(
            new EnvironmentService(_db.Object, auditService),
            new WebhookService(_db.Object));
    }

    private const string MissingApiKey = "missing-key";

    // ─── Core CRUD ──────────────────────────────────────────────────────────

    [Test]
    public async Task WhenListingEnvironments_ThenPageSizeIsClampedAndResultsAreMapped()
    {
        var result = await _controller.List(ProjectId, page: 1, pageSize: 1000);

        var ok = result.Result as OkObjectResult;
        var paged = ok!.Value as PaginatedResponse<EnvironmentResponse>;
        Assert.That(paged!.Count, Is.EqualTo(1));
        Assert.That(paged.Results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAnEnvironment_ThenItIsPersistedAndReturned()
    {
        var result = await _controller.Create(new CreateEnvironmentRequest("Staging", ProjectId), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var body = created!.Value as EnvironmentResponse;
        Assert.That(body!.Name, Is.EqualTo("Staging"));
    }

    [Test]
    public async Task WhenGettingByApiKeyThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetByApiKey(MissingApiKey, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingByApiKeyThatExists_ThenTheEnvironmentIsReturned()
    {
        var result = await _controller.GetByApiKey(_environment.ApiKey, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as EnvironmentResponse;
        Assert.That(body!.Id, Is.EqualTo(EnvironmentId));
    }

    [Test]
    public async Task WhenUpdatingAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Update(MissingApiKey, new UpdateEnvironmentRequest("Renamed"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAnEnvironmentThatExists_ThenTheNameIsChanged()
    {
        var result = await _controller.Update(_environment.ApiKey, new UpdateEnvironmentRequest("Renamed"), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as EnvironmentResponse;
        Assert.That(body!.Name, Is.EqualTo("Renamed"));
    }

    [Test]
    public async Task WhenDeletingAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Delete(MissingApiKey, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAnEnvironmentThatExists_ThenNoContentIsReturned()
    {
        var result = await _controller.Delete(_environment.ApiKey, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_environments, Is.Empty);
    }

    [Test]
    public async Task WhenCloningAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.Clone(MissingApiKey, new CloneEnvironmentRequest("Clone"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenCloningAnEnvironmentThatExists_ThenTheCloneIsReturned()
    {
        var result = await _controller.Clone(_environment.ApiKey, new CloneEnvironmentRequest("Clone"), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        var body = created!.Value as EnvironmentResponse;
        Assert.That(body!.Name, Is.EqualTo("Clone"));
        Assert.That(body.ApiKey, Is.Not.EqualTo(_environment.ApiKey));
    }

    // ─── Webhooks ───────────────────────────────────────────────────────────

    [Test]
    public async Task WhenListingWebhooksForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListWebhooks(MissingApiKey, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingWebhooksForAnEnvironmentThatExists_ThenTheyAreReturned()
    {
        _webhooks.Add(new Webhook { Id = 1, Url = "https://example.com", Scope = WebhookScope.Environment, EnvironmentId = EnvironmentId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListWebhooks(_environment.ApiKey, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as IReadOnlyList<WebhookResponse>;
        Assert.That(body, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAWebhookForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.CreateWebhook(MissingApiKey, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenCreatingAWebhookForAnEnvironmentThatExists_ThenItIsPersisted()
    {
        var result = await _controller.CreateWebhook(_environment.ApiKey, new CreateWebhookRequest("https://example.com", "secret", true), CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        var body = created!.Value as WebhookResponse;
        Assert.That(body!.Url, Is.EqualTo("https://example.com"));
        Assert.That(_webhooks, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenUpdatingAWebhookForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpdateWebhook(MissingApiKey, 1, new CreateWebhookRequest("https://example.com", null, true), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAWebhookThatBelongsToAnotherEnvironment_ThenNotFoundIsReturned()
    {
        _webhooks.Add(new Webhook { Id = 1, Url = "https://example.com", Scope = WebhookScope.Environment, EnvironmentId = 999, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.UpdateWebhook(_environment.ApiKey, 1, new CreateWebhookRequest("https://updated.com", null, true), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAWebhookThatBelongsToTheEnvironment_ThenItIsUpdated()
    {
        _webhooks.Add(new Webhook { Id = 1, Url = "https://example.com", Scope = WebhookScope.Environment, EnvironmentId = EnvironmentId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.UpdateWebhook(_environment.ApiKey, 1, new CreateWebhookRequest("https://updated.com", null, false), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as WebhookResponse;
        Assert.That(body!.Url, Is.EqualTo("https://updated.com"));
    }

    [Test]
    public async Task WhenListingWebhookDeliveriesForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListWebhookDeliveries(MissingApiKey, 1, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingWebhookDeliveriesForAWebhookThatBelongsToAnotherEnvironment_ThenNotFoundIsReturned()
    {
        _webhooks.Add(new Webhook { Id = 1, Url = "https://example.com", Scope = WebhookScope.Environment, EnvironmentId = 999, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListWebhookDeliveries(_environment.ApiKey, 1, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingWebhookDeliveriesForAWebhookThatBelongsToTheEnvironment_ThenTheyAreReturned()
    {
        _webhooks.Add(new Webhook { Id = 1, Url = "https://example.com", Scope = WebhookScope.Environment, EnvironmentId = EnvironmentId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });
        _webhookDeliveryLogs.Add(new WebhookDeliveryLog
        {
            Id = 1, WebhookId = 1, EventType = "flag.updated", PayloadJson = "{}", Success = true, AttemptNumber = 1, AttemptedAt = DateTimeOffset.UtcNow
        });

        var result = await _controller.ListWebhookDeliveries(_environment.ApiKey, 1, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as IReadOnlyList<WebhookDeliveryLogResponse>;
        Assert.That(body, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenDeletingAWebhookForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteWebhook(MissingApiKey, 1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAWebhookThatBelongsToAnotherEnvironment_ThenNotFoundIsReturned()
    {
        _webhooks.Add(new Webhook { Id = 1, Url = "https://example.com", Scope = WebhookScope.Environment, EnvironmentId = 999, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.DeleteWebhook(_environment.ApiKey, 1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAWebhookThatBelongsToTheEnvironment_ThenNoContentIsReturned()
    {
        _webhooks.Add(new Webhook { Id = 1, Url = "https://example.com", Scope = WebhookScope.Environment, EnvironmentId = EnvironmentId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.DeleteWebhook(_environment.ApiKey, 1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_webhooks, Is.Empty);
    }

    // ─── Audit logs ─────────────────────────────────────────────────────────

    [Test]
    public async Task WhenListingAuditLogsForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListAuditLogs(MissingApiKey, _auditLogQueryService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingAuditLogsForAnEnvironmentThatExists_ThenTheyAreReturned()
    {
        _auditLogs.Add(new AuditLog { Id = 1, ResourceType = "Environment", ResourceId = "1", Action = "updated", OrganizationId = OrganizationId, EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListAuditLogs(_environment.ApiKey, _auditLogQueryService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as List<AuditLogResponse>;
        Assert.That(body, Has.Count.EqualTo(1));
    }

    // ─── Identities ─────────────────────────────────────────────────────────

    [Test]
    public async Task WhenListingIdentitiesForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListIdentities(MissingApiKey, _adminIdentityService, page: 1, pageSize: 1000, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingIdentitiesForAnEnvironmentThatExists_ThenPageSizeIsClampedAndResultsMapped()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListIdentities(_environment.ApiKey, _adminIdentityService, page: 1, pageSize: 1000, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as PaginatedResponse<AdminIdentityResponse>;
        Assert.That(body!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAnIdentityForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.CreateIdentity(MissingApiKey, new CreateIdentityRequest("user-1"), _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenCreatingAnIdentityThatAlreadyExists_ThenConflictIsReturned()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.CreateIdentity(_environment.ApiKey, new CreateIdentityRequest("user-1"), _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task WhenCreatingANewIdentity_ThenItIsPersisted()
    {
        var result = await _controller.CreateIdentity(_environment.ApiKey, new CreateIdentityRequest("user-1"), _adminIdentityService, CancellationToken.None);

        var created = result.Result as CreatedAtActionResult;
        var body = created!.Value as AdminIdentityResponse;
        Assert.That(body!.Identifier, Is.EqualTo("user-1"));
    }

    [Test]
    public async Task WhenGettingAnIdentityForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetIdentity(MissingApiKey, 1, _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingAnIdentityThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetIdentity(_environment.ApiKey, 999, _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingAnIdentityThatExists_ThenItIsReturned()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.GetIdentity(_environment.ApiKey, 1, _adminIdentityService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as AdminIdentityResponse;
        Assert.That(body!.Identifier, Is.EqualTo("user-1"));
    }

    [Test]
    public async Task WhenDeletingAnIdentityForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteIdentity(MissingApiKey, 1, _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAnIdentityThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteIdentity(_environment.ApiKey, 999, _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAnIdentityThatExists_ThenNoContentIsReturned()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.DeleteIdentity(_environment.ApiKey, 1, _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_identities, Is.Empty);
    }

    [Test]
    public async Task WhenUpsertingATraitForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpsertIdentityTrait(MissingApiKey, 1, "plan", new UpsertTraitRequest("pro"), _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpsertingATraitForAnIdentityThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpsertIdentityTrait(_environment.ApiKey, 999, "plan", new UpsertTraitRequest("pro"), _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpsertingATraitForAnIdentityThatExists_ThenTheTraitIsReturned()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.UpsertIdentityTrait(_environment.ApiKey, 1, "plan", new UpsertTraitRequest("pro"), _adminIdentityService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as TraitResponse;
        Assert.That(body!.Value, Is.EqualTo("pro"));
    }

    [Test]
    public async Task WhenDeletingATraitForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteIdentityTrait(MissingApiKey, 1, "plan", _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingATraitThatDoesNotExist_ThenNotFoundIsReturned()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.DeleteIdentityTrait(_environment.ApiKey, 1, "plan", _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingATraitThatExists_ThenNoContentIsReturned()
    {
        var identity = new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow };
        _identities.Add(identity);
        await _adminIdentityService.UpsertTraitAsync(1, EnvironmentId, "plan", "pro");

        var result = await _controller.DeleteIdentityTrait(_environment.ApiKey, 1, "plan", _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task WhenGettingFeatureStatesForAnIdentityInAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetIdentityFeatureStates(MissingApiKey, 1, _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingFeatureStatesForAnIdentityThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetIdentityFeatureStates(_environment.ApiKey, 999, _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingFeatureStatesForAnIdentityThatExists_ThenTheyAreReturned()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });
        _featureStates.Add(new FeatureState { Id = 1, FeatureId = 1, EnvironmentId = EnvironmentId, IdentityId = 1, Enabled = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.GetIdentityFeatureStates(_environment.ApiKey, 1, _adminIdentityService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as IReadOnlyList<FeatureStateResponse>;
        Assert.That(body, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenSettingAFeatureStateForAnIdentityInAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.SetIdentityFeatureState(MissingApiKey, 1, 1, new UpdateFeatureStateRequest(true, "on"), _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenSettingAFeatureStateForAnIdentityThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.SetIdentityFeatureState(_environment.ApiKey, 999, 1, new UpdateFeatureStateRequest(true, "on"), _adminIdentityService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenSettingAFeatureStateForAnIdentityThatExists_ThenTheStateIsPersisted()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.SetIdentityFeatureState(_environment.ApiKey, 1, 1, new UpdateFeatureStateRequest(true, "on"), _adminIdentityService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as FeatureStateResponse;
        Assert.That(body!.Enabled, Is.True);
        Assert.That(body.Value, Is.EqualTo("on"));
    }

    [Test]
    public async Task WhenDeletingAFeatureStateForAnIdentityInAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteIdentityFeatureState(MissingApiKey, 1, 1, _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAFeatureStateForAnIdentityThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteIdentityFeatureState(_environment.ApiKey, 999, 1, _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAFeatureStateForAnIdentityThatExists_ThenNoContentIsReturned()
    {
        _identities.Add(new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow });
        _featureStates.Add(new FeatureState { Id = 1, FeatureId = 1, EnvironmentId = EnvironmentId, IdentityId = 1, Enabled = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.DeleteIdentityFeatureState(_environment.ApiKey, 1, 1, _adminIdentityService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_featureStates, Is.Empty);
    }

    // ─── Environment-level feature states ──────────────────────────────────

    [Test]
    public async Task WhenListingFeatureStatesForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListFeatureStates(MissingApiKey, _featureStateService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingFeatureStatesForAnEnvironmentThatExists_ThenTheyAreReturned()
    {
        _featureStates.Add(new FeatureState { Id = 1, FeatureId = 1, EnvironmentId = EnvironmentId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.ListFeatureStates(_environment.ApiKey, _featureStateService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as IReadOnlyList<FeatureStateResponse>;
        Assert.That(body, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenGettingAFeatureStateForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetFeatureState(MissingApiKey, 1, _featureStateService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingAFeatureStateThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetFeatureState(_environment.ApiKey, 999, _featureStateService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingAFeatureStateThatExists_ThenItIsReturned()
    {
        _featureStates.Add(new FeatureState { Id = 1, FeatureId = 1, EnvironmentId = EnvironmentId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.GetFeatureState(_environment.ApiKey, 1, _featureStateService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as FeatureStateResponse;
        Assert.That(body!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenUpdatingAFeatureStateForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpdateFeatureState(MissingApiKey, 1, new UpdateFeatureStateRequest(true, "on"), _featureStateService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAFeatureStateThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpdateFeatureState(_environment.ApiKey, 999, new UpdateFeatureStateRequest(true, "on"), _featureStateService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAFeatureStateThatExists_ThenItIsUpdated()
    {
        _features.Add(new Feature { Id = 1, Name = "flag", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });
        _featureStates.Add(new FeatureState { Id = 1, FeatureId = 1, EnvironmentId = EnvironmentId, Enabled = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.UpdateFeatureState(_environment.ApiKey, 1, new UpdateFeatureStateRequest(true, "on"), _featureStateService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as FeatureStateResponse;
        Assert.That(body!.Enabled, Is.True);
    }

    [Test]
    public async Task WhenPatchingAFeatureStateForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.PatchFeatureState(MissingApiKey, 1, new PatchFeatureStateRequest(true, null), _featureStateService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenPatchingAFeatureStateThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.PatchFeatureState(_environment.ApiKey, 999, new PatchFeatureStateRequest(true, null), _featureStateService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenPatchingAFeatureStateThatExists_ThenOnlyProvidedFieldsAreChanged()
    {
        _features.Add(new Feature { Id = 1, Name = "flag", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });
        _featureStates.Add(new FeatureState { Id = 1, FeatureId = 1, EnvironmentId = EnvironmentId, Enabled = false, Value = "original", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.PatchFeatureState(_environment.ApiKey, 1, new PatchFeatureStateRequest(true, null), _featureStateService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as FeatureStateResponse;
        Assert.That(body!.Enabled, Is.True);
        Assert.That(body.Value, Is.EqualTo("original"));
    }

    // ─── Feature segments ───────────────────────────────────────────────────

    [Test]
    public async Task WhenListingFeatureSegmentsForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.ListFeatureSegments(MissingApiKey, 1, _featureSegmentService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenListingFeatureSegmentsForAnEnvironmentThatExists_ThenTheyAreReturned()
    {
        _segments.Add(new Segment { Id = 1, Name = "Beta", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });
        _featureSegments.Add(new FeatureSegment { Id = 1, FeatureId = 1, SegmentId = 1, EnvironmentId = EnvironmentId, Priority = 1 });

        var result = await _controller.ListFeatureSegments(_environment.ApiKey, 1, _featureSegmentService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as IReadOnlyList<FeatureSegmentResponse>;
        Assert.That(body, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAFeatureSegmentForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.CreateFeatureSegment(MissingApiKey, 1, new CreateFeatureSegmentRequest(1, 1, true, null), _featureSegmentService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenCreatingAFeatureSegmentForASegmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.CreateFeatureSegment(_environment.ApiKey, 1, new CreateFeatureSegmentRequest(999, 1, true, null), _featureSegmentService, CancellationToken.None);

        var notFound = result.Result as NotFoundObjectResult;
        Assert.That(notFound, Is.Not.Null);
    }

    [Test]
    public async Task WhenCreatingAFeatureSegmentForASegmentThatExists_ThenItIsPersisted()
    {
        _segments.Add(new Segment { Id = 1, Name = "Beta", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _controller.CreateFeatureSegment(_environment.ApiKey, 1, new CreateFeatureSegmentRequest(1, 1, true, "on"), _featureSegmentService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as FeatureSegmentResponse;
        Assert.That(body!.SegmentName, Is.EqualTo("Beta"));
    }

    [Test]
    public async Task WhenUpdatingAFeatureSegmentForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpdateFeatureSegment(MissingApiKey, 1, 1, new UpdateFeatureSegmentRequest(1, true, null), _featureSegmentService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAFeatureSegmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.UpdateFeatureSegment(_environment.ApiKey, 1, 999, new UpdateFeatureSegmentRequest(1, true, null), _featureSegmentService, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenUpdatingAFeatureSegmentThatExists_ThenItIsUpdated()
    {
        _segments.Add(new Segment { Id = 1, Name = "Beta", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });
        _featureSegments.Add(new FeatureSegment { Id = 1, FeatureId = 1, SegmentId = 1, EnvironmentId = EnvironmentId, Priority = 1 });

        var result = await _controller.UpdateFeatureSegment(_environment.ApiKey, 1, 1, new UpdateFeatureSegmentRequest(5, true, "on"), _featureSegmentService, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as FeatureSegmentResponse;
        Assert.That(body!.Priority, Is.EqualTo(5));
    }

    [Test]
    public async Task WhenDeletingAFeatureSegmentForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteFeatureSegment(MissingApiKey, 1, 1, _featureSegmentService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAFeatureSegmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.DeleteFeatureSegment(_environment.ApiKey, 1, 999, _featureSegmentService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenDeletingAFeatureSegmentThatExists_ThenNoContentIsReturned()
    {
        _featureSegments.Add(new FeatureSegment { Id = 1, FeatureId = 1, SegmentId = 1, EnvironmentId = EnvironmentId, Priority = 1 });

        var result = await _controller.DeleteFeatureSegment(_environment.ApiKey, 1, 1, _featureSegmentService, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(_featureSegments, Is.Empty);
    }

    // ─── Identity segments ──────────────────────────────────────────────────

    [Test]
    public async Task WhenGettingIdentitySegmentsForAnEnvironmentThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetIdentitySegments(MissingApiKey, 1, _adminIdentityService, _segmentService, _segmentEvaluator, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingIdentitySegmentsForAnIdentityThatDoesNotExist_ThenNotFoundIsReturned()
    {
        var result = await _controller.GetIdentitySegments(_environment.ApiKey, 999, _adminIdentityService, _segmentService, _segmentEvaluator, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task WhenGettingIdentitySegments_ThenOnlyMatchingSegmentsAreReturned()
    {
        var identity = new Identity { Id = 1, Identifier = "user-1", EnvironmentId = EnvironmentId, CreatedAt = DateTimeOffset.UtcNow };
        _identities.Add(identity);
        await _adminIdentityService.UpsertTraitAsync(1, EnvironmentId, "plan", "pro");

        var matchingSegment = new Segment { Id = 1, Name = "Pro Users", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        var matchingRule = new SegmentRule { Id = 1, SegmentId = 1, Type = SegmentRuleType.All };
        matchingRule.Conditions.Add(new SegmentCondition { Id = 1, RuleId = 1, Property = "plan", Operator = SegmentConditionOperator.Equal, Value = "pro" });
        matchingSegment.Rules.Add(matchingRule);
        _segments.Add(matchingSegment);

        var nonMatchingSegment = new Segment { Id = 2, Name = "Free Users", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        var nonMatchingRule = new SegmentRule { Id = 2, SegmentId = 2, Type = SegmentRuleType.All };
        nonMatchingRule.Conditions.Add(new SegmentCondition { Id = 2, RuleId = 2, Property = "plan", Operator = SegmentConditionOperator.Equal, Value = "free" });
        nonMatchingSegment.Rules.Add(nonMatchingRule);
        _segments.Add(nonMatchingSegment);

        var result = await _controller.GetIdentitySegments(_environment.ApiKey, 1, _adminIdentityService, _segmentService, _segmentEvaluator, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var body = ok!.Value as IReadOnlyList<SegmentSummaryResponse>;
        Assert.That(body, Has.Count.EqualTo(1));
        Assert.That(body![0].Name, Is.EqualTo("Pro Users"));
    }
}
