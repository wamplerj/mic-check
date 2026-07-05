using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Identities;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Identities;

[TestFixture]
public class AdminIdentityServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Identity> _identities = null!;
    private List<IdentityTrait> _traits = null!;
    private List<FeatureState> _featureStates = null!;
    private AdminIdentityService _service = null!;
    private const int EnvironmentId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _identities = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Identities, _identities);
        _traits = [];
        var traitsSet = MockDbSetFactory.Create(_traits);
        traitsSet.Setup(m => m.Add(It.IsAny<IdentityTrait>())).Callback<IdentityTrait>(trait =>
        {
            _traits.Add(trait);
            _identities.FirstOrDefault(i => i.Id == trait.IdentityId)?.Traits.Add(trait);
        });
        traitsSet.Setup(m => m.Remove(It.IsAny<IdentityTrait>())).Callback<IdentityTrait>(trait =>
        {
            _traits.Remove(trait);
            _identities.FirstOrDefault(i => i.Id == trait.IdentityId)?.Traits.Remove(trait);
        });
        _db.Setup(c => c.IdentityTraits).Returns(traitsSet.Object);
        _featureStates = [];
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureStates, _featureStates);

        _service = new AdminIdentityService(_db.Object);
    }

    [Test]
    public async Task WhenCreatingAnIdentityWithANewIdentifier_ThenItIsPersisted()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");

        Assert.That(identity, Is.Not.Null);
        Assert.That(identity!.Identifier, Is.EqualTo("user-1"));
        Assert.That(_identities, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAnIdentityWithAnIdentifierThatAlreadyExistsInTheEnvironment_ThenNullIsReturned()
    {
        await _service.CreateAsync(EnvironmentId, "user-1");

        var result = await _service.CreateAsync(EnvironmentId, "user-1");

        Assert.That(result, Is.Null);
        Assert.That(_identities, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAnIdentityWithTheSameIdentifierInADifferentEnvironment_ThenItIsPersisted()
    {
        await _service.CreateAsync(EnvironmentId, "user-1");

        var result = await _service.CreateAsync(999, "user-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(_identities, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenListingIdentitiesForAnEnvironment_ThenOnlyThatEnvironmentsIdentitiesAreReturnedWithTheTotalCount()
    {
        await _service.CreateAsync(EnvironmentId, "user-1");
        await _service.CreateAsync(EnvironmentId, "user-2");
        await _service.CreateAsync(999, "other-env-user");

        var result = await _service.ListAsync(EnvironmentId, page: 1, pageSize: 20);

        Assert.That(result.Total, Is.EqualTo(2));
        Assert.That(result.Items, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenListingIdentitiesWithPagination_ThenTheSecondPageSkipsTheFirstPageSize()
    {
        for (var i = 0; i < 5; i++)
            await _service.CreateAsync(EnvironmentId, $"user-{i}");

        var result = await _service.ListAsync(EnvironmentId, page: 2, pageSize: 2);

        Assert.That(result.Total, Is.EqualTo(5));
        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items[0].Identifier, Is.EqualTo("user-2"));
    }

    [Test]
    public async Task WhenFindingAnIdentityByIdInTheCorrectEnvironment_ThenItIsReturned()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");

        var found = await _service.FindByIdAsync(identity!.Id, EnvironmentId);

        Assert.That(found, Is.Not.Null);
    }

    [Test]
    public async Task WhenFindingAnIdentityByIdInADifferentEnvironment_ThenNullIsReturned()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");

        var found = await _service.FindByIdAsync(identity!.Id, 999);

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task WhenUpsertingATraitForAnIdentityThatDoesNotExist_ThenNullIsReturned()
    {
        var result = await _service.UpsertTraitAsync(999, EnvironmentId, "plan", "premium");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task WhenUpsertingANewTrait_ThenItIsAddedToTheIdentity()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");

        var response = await _service.UpsertTraitAsync(identity!.Id, EnvironmentId, "plan", "premium");

        Assert.That(response!.Key, Is.EqualTo("plan"));
        Assert.That(response.Value, Is.EqualTo("premium"));
        Assert.That(_traits, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenUpsertingATraitThatAlreadyExists_ThenItsValueIsUpdatedWithoutDuplication()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");
        await _service.UpsertTraitAsync(identity!.Id, EnvironmentId, "plan", "free");

        await _service.UpsertTraitAsync(identity.Id, EnvironmentId, "plan", "premium");

        Assert.That(_traits, Has.Count.EqualTo(1));
        Assert.That(_traits[0].Value, Is.EqualTo("premium"));
    }

    [Test]
    public async Task WhenDeletingATraitFromAnIdentityThatDoesNotExist_ThenFalseIsReturned()
    {
        var result = await _service.DeleteTraitAsync(999, EnvironmentId, "plan");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task WhenDeletingATraitThatDoesNotExistOnTheIdentity_ThenFalseIsReturned()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");

        var result = await _service.DeleteTraitAsync(identity!.Id, EnvironmentId, "unknown");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task WhenDeletingATraitThatExists_ThenItIsRemovedAndTrueIsReturned()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");
        await _service.UpsertTraitAsync(identity!.Id, EnvironmentId, "plan", "premium");

        var result = await _service.DeleteTraitAsync(identity.Id, EnvironmentId, "plan");

        Assert.That(result, Is.True);
        Assert.That(_traits, Is.Empty);
    }

    [Test]
    public void WhenDeletingAnIdentityThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.DeleteAsync(999));
    }

    [Test]
    public async Task WhenDeletingAnIdentity_ThenItIsRemovedFromTheDatabase()
    {
        var identity = await _service.CreateAsync(EnvironmentId, "user-1");

        await _service.DeleteAsync(identity!.Id);

        Assert.That(_identities.Any(i => i.Id == identity.Id), Is.False);
    }

    [Test]
    public async Task WhenGettingFeatureStatesForAnIdentity_ThenOnlyThatIdentitysStatesInTheEnvironmentAreReturned()
    {
        _featureStates.Add(new FeatureState { Id = 1, FeatureId = 1, EnvironmentId = EnvironmentId, IdentityId = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        _featureStates.Add(new FeatureState { Id = 2, FeatureId = 2, EnvironmentId = EnvironmentId, IdentityId = 2, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var states = await _service.GetFeatureStatesAsync(1, EnvironmentId);

        Assert.That(states, Has.Count.EqualTo(1));
        Assert.That(states[0].FeatureId, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenSettingAFeatureStateThatDoesNotExist_ThenANewOverrideIsCreated()
    {
        var state = await _service.SetFeatureStateAsync(1, EnvironmentId, featureId: 1, enabled: true, value: "on");

        Assert.That(state.IdentityId, Is.EqualTo(1));
        Assert.That(state.Enabled, Is.True);
        Assert.That(_featureStates, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenSettingAFeatureStateThatAlreadyExists_ThenItIsUpdatedAndVersionIncremented()
    {
        var created = await _service.SetFeatureStateAsync(1, EnvironmentId, featureId: 1, enabled: false, value: "off");
        var versionBeforeUpdate = created.Version;

        var updated = await _service.SetFeatureStateAsync(1, EnvironmentId, featureId: 1, enabled: true, value: "on");

        Assert.That(_featureStates, Has.Count.EqualTo(1));
        Assert.That(updated.Enabled, Is.True);
        Assert.That(updated.Version, Is.EqualTo(versionBeforeUpdate + 1));
    }

    [Test]
    public void WhenDeletingAFeatureStateThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.DeleteFeatureStateAsync(1, EnvironmentId, featureId: 1));
    }

    [Test]
    public async Task WhenDeletingAFeatureStateThatExists_ThenItIsRemoved()
    {
        await _service.SetFeatureStateAsync(1, EnvironmentId, featureId: 1, enabled: true, value: "on");

        await _service.DeleteFeatureStateAsync(1, EnvironmentId, featureId: 1);

        Assert.That(_featureStates, Is.Empty);
    }
}
