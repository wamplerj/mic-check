using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using MicCheck.Api.Identities;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class EnvironmentServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AppEnvironment> _environments = null!;
    private List<Feature> _features = null!;
    private List<FeatureState> _featureStates = null!;
    private List<Identity> _identities = null!;
    private EnvironmentService _service = null!;
    private const int OrganizationId = 1;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _db.SetupDbSet(c => c.Organizations, [
            new Organization { Id = OrganizationId, Name = "Test Org", CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _db.SetupDbSet(c => c.Projects, [
            new Project { Id = ProjectId, Name = "Test Project", OrganizationId = OrganizationId, CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _environments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Environments, _environments);
        _features = [];
        _db.SetupDbSet(c => c.Features, _features);
        _featureStates = [];
        _db.SetupDbSet(c => c.FeatureStates, _featureStates);
        _identities = [];
        _db.SetupDbSet(c => c.Identities, _identities);

        var webhookQueue = new MicCheck.Api.Webhooks.WebhookQueue();
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new EnvironmentService(_db.Object, auditService.Object);
    }

    [Test]
    public async Task WhenCreatingAnEnvironment_ThenFeatureStateIsCreatedForEachExistingFeature()
    {
        _features.Add(new Feature
        {
            Id = 1,
            Name = "feature_a",
            ProjectId = ProjectId,
            InitialValue = "hello",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var environment = await _service.CreateAsync(ProjectId, "Staging");

        var states = _featureStates.Where(fs => fs.EnvironmentId == environment.Id).ToList();
        Assert.That(states, Has.Count.EqualTo(1));
        Assert.That(states[0].Value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task WhenCreatingAnEnvironment_ThenAUniqueApiKeyIsGenerated()
    {
        var env1 = await _service.CreateAsync(ProjectId, "Env1");
        var env2 = await _service.CreateAsync(ProjectId, "Env2");

        Assert.That(env1.ApiKey, Is.Not.EqualTo(env2.ApiKey));
    }

    [Test]
    public async Task WhenCloningAnEnvironment_ThenEnvironmentLevelFeatureStatesAreCopied()
    {
        var source = await _service.CreateAsync(ProjectId, "Production");

        var feature = new Feature { Id = 1, Name = "flag", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _features.Add(feature);

        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = source.Id,
            Enabled = true,
            Value = "prod-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var cloned = await _service.CloneAsync(source.ApiKey, "Staging");

        var clonedStates = _featureStates.Where(fs => fs.EnvironmentId == cloned.Id).ToList();

        Assert.That(clonedStates, Has.Count.EqualTo(1));
        Assert.That(clonedStates[0].Value, Is.EqualTo("prod-value"));
        Assert.That(clonedStates[0].Enabled, Is.True);
    }

    [Test]
    public async Task WhenCloningAnEnvironment_ThenIdentityOverridesAreNotCopied()
    {
        var source = await _service.CreateAsync(ProjectId, "Production");

        var feature = new Feature { Id = 1, Name = "flag", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _features.Add(feature);

        var identity = new Identity
        {
            Id = 1,
            Identifier = "user-1",
            EnvironmentId = source.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _identities.Add(identity);

        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = source.Id,
            IdentityId = identity.Id,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var cloned = await _service.CloneAsync(source.ApiKey, "Staging");

        var clonedStates = _featureStates.Where(fs => fs.EnvironmentId == cloned.Id).ToList();

        Assert.That(clonedStates, Is.Empty);
    }

    [Test]
    public async Task WhenCloningAnEnvironment_ThenClonedEnvironmentHasDifferentApiKey()
    {
        var source = await _service.CreateAsync(ProjectId, "Production");

        var cloned = await _service.CloneAsync(source.ApiKey, "Staging");

        Assert.That(cloned.ApiKey, Is.Not.EqualTo(source.ApiKey));
    }

    [Test]
    public void WhenCloningFromANonExistentApiKey_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.That(async () => await _service.CloneAsync("missing-key", "Staging"), Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public async Task WhenListingByProject_ThenOnlyEnvironmentsForThatProjectAreReturned()
    {
        await _service.CreateAsync(ProjectId, "Env1");
        await _service.CreateAsync(999, "OtherProjectEnv");

        var result = await _service.ListByProjectAsync(ProjectId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Env1"));
    }

    [Test]
    public async Task WhenFindingByApiKeyThatDoesNotExist_ThenNullIsReturned()
    {
        var result = await _service.FindByApiKeyAsync("missing-key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task WhenFindingByApiKeyThatExists_ThenTheEnvironmentIsReturned()
    {
        var created = await _service.CreateAsync(ProjectId, "Production");

        var result = await _service.FindByApiKeyAsync(created.ApiKey);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task WhenUpdatingAnEnvironment_ThenNameIsChanged()
    {
        var created = await _service.CreateAsync(ProjectId, "Original");

        var updated = await _service.UpdateAsync(created.ApiKey, "Renamed");

        Assert.That(updated.Name, Is.EqualTo("Renamed"));
    }

    [Test]
    public void WhenUpdatingANonExistentEnvironment_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.That(async () => await _service.UpdateAsync("missing-key", "Renamed"), Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public async Task WhenDeletingAnExistingEnvironment_ThenItIsRemoved()
    {
        var created = await _service.CreateAsync(ProjectId, "ToDelete");

        await _service.DeleteAsync(created.ApiKey);

        Assert.That(_environments, Is.Empty);
    }

    [Test]
    public void WhenDeletingANonExistentEnvironment_ThenNoExceptionIsThrown()
    {
        Assert.That(async () => await _service.DeleteAsync("missing-key"), Throws.Nothing);
    }
}
