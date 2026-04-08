using MicCheck.Api.Audit;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class EnvironmentServiceTests
{
    private MicCheckDbContext _db = null!;
    private EnvironmentService _service = null!;
    private const int OrganizationId = 1;
    private const int ProjectId = 1;

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

        _service = new EnvironmentService(_db, auditService.Object);

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
        _db.SaveChanges();
    }

    [Test]
    public async Task WhenCreatingAnEnvironment_ThenFeatureStateIsCreatedForEachExistingFeature()
    {
        _db.Features.Add(new Feature
        {
            Name = "feature_a",
            ProjectId = ProjectId,
            InitialValue = "hello",
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

        var environment = await _service.CreateAsync(ProjectId, "Staging");

        var states = await _db.FeatureStates.Where(fs => fs.EnvironmentId == environment.Id).ToListAsync();
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

        var feature = new Feature { Name = "flag", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _db.Features.Add(feature);
        _db.SaveChanges();

        _db.FeatureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = source.Id,
            Enabled = true,
            Value = "prod-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

        var cloned = await _service.CloneAsync(source.ApiKey, "Staging");

        var clonedStates = await _db.FeatureStates
            .Where(fs => fs.EnvironmentId == cloned.Id)
            .ToListAsync();

        Assert.That(clonedStates, Has.Count.EqualTo(1));
        Assert.That(clonedStates[0].Value, Is.EqualTo("prod-value"));
        Assert.That(clonedStates[0].Enabled, Is.True);
    }

    [Test]
    public async Task WhenCloningAnEnvironment_ThenIdentityOverridesAreNotCopied()
    {
        var source = await _service.CreateAsync(ProjectId, "Production");

        var feature = new Feature { Name = "flag", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _db.Features.Add(feature);
        _db.SaveChanges();

        var identity = new MicCheck.Api.Identities.Identity
        {
            Identifier = "user-1",
            EnvironmentId = source.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Identities.Add(identity);
        _db.SaveChanges();

        _db.FeatureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = source.Id,
            IdentityId = identity.Id,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

        var cloned = await _service.CloneAsync(source.ApiKey, "Staging");

        var clonedStates = await _db.FeatureStates
            .Where(fs => fs.EnvironmentId == cloned.Id)
            .ToListAsync();

        Assert.That(clonedStates, Is.Empty);
    }

    [Test]
    public async Task WhenCloningAnEnvironment_ThenClonedEnvironmentHasDifferentApiKey()
    {
        var source = await _service.CreateAsync(ProjectId, "Production");

        var cloned = await _service.CloneAsync(source.ApiKey, "Staging");

        Assert.That(cloned.ApiKey, Is.Not.EqualTo(source.ApiKey));
    }
}
