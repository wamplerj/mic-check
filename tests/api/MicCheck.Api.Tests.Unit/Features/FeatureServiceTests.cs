using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Common;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureServiceTests
{
    private MicCheckDbContext _db = null!;
    private FeatureService _service = null!;
    private const int OrganizationId = 1;
    private const int ProjectId = 1;
    private const int EnvironmentId = 1;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new MicCheckDbContext(options);

        var auditService = new Mock<AuditService>(_db, null!);
        auditService.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new FeatureService(_db, auditService.Object);

        SeedBaseData();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void SeedBaseData()
    {
        _db.Organizations.Add(new MicCheck.Api.Organizations.Organization
        {
            Id = OrganizationId,
            Name = "Test Org",
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.Projects.Add(new MicCheck.Api.Projects.Project
        {
            Id = ProjectId,
            Name = "Test Project",
            OrganizationId = OrganizationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.Environments.Add(new AppEnvironment
        {
            Id = EnvironmentId,
            Name = "Production",
            ApiKey = "test-key",
            ProjectId = ProjectId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    [Test]
    public async Task WhenCreatingAFeature_ThenFeatureStateIsCreatedForEachEnvironment()
    {
        var feature = await _service.CreateAsync(ProjectId, "dark_mode", FeatureType.Standard, null, null);

        var states = await _db.FeatureStates.Where(fs => fs.FeatureId == feature.Id).ToListAsync();
        Assert.That(states, Has.Count.EqualTo(1));
        Assert.That(states[0].EnvironmentId, Is.EqualTo(EnvironmentId));
    }

    [Test]
    public async Task WhenCreatingAFeatureWithInitialValue_ThenFeatureStateHasValue()
    {
        var feature = await _service.CreateAsync(ProjectId, "flag", FeatureType.Standard, "initial-val", null);

        var state = await _db.FeatureStates.FirstAsync(fs => fs.FeatureId == feature.Id);
        Assert.That(state.Value, Is.EqualTo("initial-val"));
    }

    [Test]
    public async Task WhenProjectHas400Features_ThenCreatingAnotherThrowsDomainException()
    {
        for (var i = 0; i < 400; i++)
        {
            _db.Features.Add(new Feature
            {
                Name = $"feature_{i}",
                ProjectId = ProjectId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        _db.SaveChanges();

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

        var found = await _db.Features.FirstOrDefaultAsync(f => f.Id == feature.Id);
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
}
