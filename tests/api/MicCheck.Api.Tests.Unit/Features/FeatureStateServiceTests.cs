using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Webhooks;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureStateServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<FeatureState> _featureStates = null!;
    private List<Feature> _features = null!;
    private List<AppEnvironment> _environments = null!;
    private FeatureStateService _service = null!;
    private const int OrganizationId = 1;
    private const int ProjectId = 1;
    private const int EnvironmentId = 1;
    private const int FeatureId = 1;

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
        _environments = [new AppEnvironment { Id = EnvironmentId, Name = "Production", ApiKey = "test-key", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Environments, _environments);
        _features = [new Feature { Id = FeatureId, Name = "dark_mode", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Features, _features);
        _featureStates = [];
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureStates, _featureStates);

        var webhookQueue = new WebhookQueue();
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.RecordAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new FeatureStateService(_db.Object, webhookQueue, auditService.Object);
    }

    private FeatureState AddState(bool enabled = false, string? value = null)
    {
        var state = new FeatureState
        {
            Id = 1,
            FeatureId = FeatureId,
            EnvironmentId = EnvironmentId,
            Enabled = enabled,
            Value = value,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _featureStates.Add(state);
        return state;
    }

    [Test]
    public async Task WhenListingEnvironmentLevelStates_ThenIdentityAndSegmentOverridesAreExcluded()
    {
        AddState();
        _featureStates.Add(new FeatureState { Id = 2, FeatureId = FeatureId, EnvironmentId = EnvironmentId, IdentityId = 5, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        _featureStates.Add(new FeatureState { Id = 3, FeatureId = FeatureId, EnvironmentId = EnvironmentId, FeatureSegmentId = 7, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var results = await _service.ListByEnvironmentAsync(EnvironmentId);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Id, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenFindingAStateByIdInTheCorrectEnvironment_ThenItIsReturned()
    {
        var state = AddState();

        var found = await _service.FindByIdAsync(state.Id, EnvironmentId);

        Assert.That(found, Is.Not.Null);
    }

    [Test]
    public async Task WhenFindingAStateByIdInADifferentEnvironment_ThenNullIsReturned()
    {
        var state = AddState();

        var found = await _service.FindByIdAsync(state.Id, 999);

        Assert.That(found, Is.Null);
    }

    [Test]
    public void WhenUpdatingAStateThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(999, true, "on"));
    }

    [Test]
    public async Task WhenUpdatingAState_ThenEnabledAndValueAreChangedAndVersionIsIncremented()
    {
        var state = AddState(enabled: false, value: "old");

        var updated = await _service.UpdateAsync(state.Id, true, "new");

        Assert.That(updated.Enabled, Is.True);
        Assert.That(updated.Value, Is.EqualTo("new"));
        Assert.That(updated.Version, Is.EqualTo(1));
    }

    [Test]
    public void WhenPatchingAStateThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.PatchAsync(999, true, null));
    }

    [Test]
    public async Task WhenPatchingOnlyTheEnabledField_ThenValueIsUnchanged()
    {
        var state = AddState(enabled: false, value: "original");

        var patched = await _service.PatchAsync(state.Id, true, null);

        Assert.That(patched.Enabled, Is.True);
        Assert.That(patched.Value, Is.EqualTo("original"));
    }

    [Test]
    public async Task WhenPatchingOnlyTheValueField_ThenEnabledIsUnchanged()
    {
        var state = AddState(enabled: true, value: "original");

        var patched = await _service.PatchAsync(state.Id, null, "updated");

        Assert.That(patched.Enabled, Is.True);
        Assert.That(patched.Value, Is.EqualTo("updated"));
    }

    [Test]
    public async Task WhenUpdatingAState_ThenAWebhookEventIsEnqueued()
    {
        var state = AddState();
        var queue = new WebhookQueue();

        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.RecordAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new FeatureStateService(_db.Object, queue, auditService.Object);

        await service.UpdateAsync(state.Id, true, "on");

        using var cts = new CancellationTokenSource();
        await using var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.That(await enumerator.MoveNextAsync(), Is.True);
        Assert.That(enumerator.Current.EventType, Is.EqualTo(WebhookEventTypes.FlagUpdated));
        Assert.That(enumerator.Current.EnvironmentId, Is.EqualTo(EnvironmentId));
        Assert.That(enumerator.Current.OrganizationId, Is.EqualTo(OrganizationId));
    }
}
