using MicCheck.Api.Audit;
using MicCheck.Api.Common;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Project> _projects = null!;
    private List<Feature> _features = null!;
    private List<FeatureState> _featureStates = null!;
    private List<Tag> _tags = null!;
    private FeatureService _service = null!;
    private const int OrganizationId = 1;
    private const int ProjectId = 1;
    private const int EnvironmentId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _db.SetupDbSet(c => c.Organizations, [
            new Organization { Id = OrganizationId, Name = "Test Org", CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _projects = [new Project { Id = ProjectId, Name = "Test Project", OrganizationId = OrganizationId, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Projects, _projects);
        _db.SetupDbSet(c => c.Environments, [
            new AppEnvironment { Id = EnvironmentId, Name = "Production", ApiKey = "test-key", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _features = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Features, _features);
        _featureStates = [];
        _db.SetupDbSet(c => c.FeatureStates, _featureStates);
        _tags = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Tags, _tags);

        var webhookQueue = new MicCheck.Api.Webhooks.WebhookQueue();
        var auditService = new Mock<IAuditService>();
        auditService.Setup(a => a.RecordAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new FeatureService(_db.Object, auditService.Object, webhookQueue);
    }

    [Test]
    public async Task WhenCreatingAFeature_ThenFeatureStateIsCreatedForEachEnvironment()
    {
        var feature = await _service.CreateAsync(ProjectId, "dark_mode", FeatureType.Standard, null, null);

        var states = _featureStates.Where(fs => fs.FeatureId == feature.Id).ToList();
        Assert.That(states, Has.Count.EqualTo(1));
        Assert.That(states[0].EnvironmentId, Is.EqualTo(EnvironmentId));
    }

    [Test]
    public async Task WhenCreatingAFeatureWithInitialValue_ThenFeatureStateHasValue()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, "initial-val", null);

        var state = _featureStates.First(fs => fs.FeatureId == feature.Id);
        Assert.That(state.Value, Is.EqualTo("initial-val"));
    }

    [Test]
    public void WhenProjectHas400Features_ThenCreatingAnotherThrowsDomainException()
    {
        for (var i = 0; i < 400; i++)
        {
            _features.Add(new Feature
            {
                Id = i + 1,
                Name = $"feature_{i}",
                ProjectId = ProjectId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(ProjectId, "overflow_feature", FeatureType.Standard, null, null));
    }

    [Test]
    public async Task WhenUpdatingAFeature_ThenNameAndDescriptionAreChanged()
    {
        var feature = await _service.CreateAsync(ProjectId, "old_name", FeatureType.Standard, null, "old desc");

        var updated = await _service.UpdateAsync(feature.Id, "new_name", "new desc");

        Assert.That(updated.Name, Is.EqualTo("new_name"));
        Assert.That(updated.Description, Is.EqualTo("new desc"));
    }

    [Test]
    public async Task WhenDeletingAFeature_ThenItIsRemovedFromTheDatabase()
    {
        var feature = await _service.CreateAsync(ProjectId, "to_delete", FeatureType.Standard, null, null);

        await _service.DeleteAsync(feature.Id);

        var found = _features.FirstOrDefault(f => f.Id == feature.Id);
        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task WhenListingFeatures_ThenAllProjectFeaturesAreReturned()
    {
        await _service.CreateAsync(ProjectId, "feature_a", FeatureType.Standard, null, null);
        await _service.CreateAsync(ProjectId, "feature_b", FeatureType.Standard, null, null);

        var features = await _service.ListByProjectAsync(ProjectId);

        Assert.That(features, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WhenAssigningATag_ThenItAppearsOnTheFeature()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, null, null);
        var tag = new Tag { Label = "beta", Color = "#FF0000", ProjectId = ProjectId };
        _db.Object.Tags.Add(tag);

        var updated = await _service.AssignTagAsync(feature.Id, tag.Id);

        Assert.That(updated.Tags.Select(t => t.Id), Does.Contain(tag.Id));
    }

    [Test]
    public async Task WhenAssigningATagAlreadyOnTheFeature_ThenItIsNotDuplicated()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, null, null);
        var tag = new Tag { Label = "beta", Color = "#FF0000", ProjectId = ProjectId };
        _db.Object.Tags.Add(tag);

        await _service.AssignTagAsync(feature.Id, tag.Id);
        var updated = await _service.AssignTagAsync(feature.Id, tag.Id);

        Assert.That(updated.Tags.Count(t => t.Id == tag.Id), Is.EqualTo(1));
    }

    [Test]
    public async Task WhenAssigningATagFromAnotherProject_ThenDomainExceptionIsThrown()
    {
        _projects.Add(new Project
        {
            Id = 2,
            Name = "Other Project",
            OrganizationId = OrganizationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, null, null);
        var foreignTag = new Tag { Label = "beta", Color = "#FF0000", ProjectId = 2 };
        _db.Object.Tags.Add(foreignTag);

        Assert.ThrowsAsync<DomainException>(() => _service.AssignTagAsync(feature.Id, foreignTag.Id));
    }

    [Test]
    public async Task WhenRemovingAnAssignedTag_ThenItNoLongerAppearsOnTheFeature()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, null, null);
        var tag = new Tag { Label = "beta", Color = "#FF0000", ProjectId = ProjectId };
        _db.Object.Tags.Add(tag);
        await _service.AssignTagAsync(feature.Id, tag.Id);

        var updated = await _service.RemoveTagAsync(feature.Id, tag.Id);

        Assert.That(updated.Tags.Select(t => t.Id), Does.Not.Contain(tag.Id));
    }

    [Test]
    public async Task WhenFindingAFeatureByIdThatExists_ThenTheFeatureIsReturned()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, null, null);

        var found = await _service.FindByIdAsync(feature.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(feature.Id));
    }

    [Test]
    public async Task WhenFindingAFeatureByIdThatDoesNotExist_ThenNullIsReturned()
    {
        var found = await _service.FindByIdAsync(999);

        Assert.That(found, Is.Null);
    }

    [Test]
    public void WhenUpdatingAFeatureThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(999, "renamed", null));
    }

    [Test]
    public void WhenAssigningATagToAFeatureThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        var tag = new Tag { Label = "beta", Color = "#FF0000", ProjectId = ProjectId };
        _db.Object.Tags.Add(tag);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AssignTagAsync(999, tag.Id));
    }

    [Test]
    public async Task WhenAssigningATagThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, null, null);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AssignTagAsync(feature.Id, 999));
    }

    [Test]
    public void WhenRemovingATagFromAFeatureThatDoesNotExist_ThenKeyNotFoundExceptionIsThrown()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RemoveTagAsync(999, 1));
    }

    [Test]
    public async Task WhenRemovingATagThatIsNotAssigned_ThenTheFeatureIsUnchanged()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, null, null);

        var updated = await _service.RemoveTagAsync(feature.Id, 999);

        Assert.That(updated.Tags, Is.Empty);
    }

    [Test]
    public void WhenDeletingAFeatureThatDoesNotExist_ThenNoExceptionIsThrown()
    {
        Assert.DoesNotThrowAsync(() => _service.DeleteAsync(999));
    }
}
