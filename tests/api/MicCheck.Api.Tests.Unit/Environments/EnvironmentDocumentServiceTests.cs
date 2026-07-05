using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class EnvironmentDocumentServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AppEnvironment> _environments = null!;
    private List<Project> _projects = null!;
    private List<Feature> _features = null!;
    private List<FeatureState> _featureStates = null!;
    private EnvironmentDocumentService _service = null!;
    private const int ProjectId = 1;
    private const int EnvironmentId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _environments = [new AppEnvironment { Id = EnvironmentId, Name = "Production", ApiKey = "env-key", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }];
        var environmentsSet = MockDbSetFactory.Create(_environments);
        environmentsSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken _) => _environments.FirstOrDefault(e => e.Id == (int)keys[0]));
        _db.Setup(c => c.Environments).Returns(environmentsSet.Object);

        _projects = [new Project { Id = ProjectId, Name = "Test Project", OrganizationId = 1, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Projects, _projects);
        _db.SetupDbSet(c => c.Organizations, [new Organization { Id = 1, Name = "Org", CreatedAt = DateTimeOffset.UtcNow }]);

        _features = [];
        _db.SetupDbSet(c => c.Features, _features);
        _featureStates = [];
        _db.SetupDbSet(c => c.FeatureStates, _featureStates);

        _service = new EnvironmentDocumentService(_db.Object);
    }

    [Test]
    public async Task WhenTheEnvironmentDoesNotExist_ThenNullIsReturned()
    {
        var result = await _service.GetAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task WhenTheEnvironmentsProjectDoesNotExist_ThenNullIsReturned()
    {
        _projects.Clear();

        var result = await _service.GetAsync(EnvironmentId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task WhenTheEnvironmentExists_ThenEnvironmentLevelFeatureStatesAreIncluded()
    {
        var feature = new Feature { Id = 1, Name = "flag_a", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _features.Add(feature);
        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            Enabled = true,
            Value = "on",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var result = await _service.GetAsync(EnvironmentId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(EnvironmentId));
        Assert.That(result.ApiKey, Is.EqualTo("env-key"));
        Assert.That(result.FeatureStates, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenTheEnvironmentExists_ThenIdentityAndSegmentScopedFeatureStatesAreExcluded()
    {
        var feature = new Feature { Id = 1, Name = "flag_a", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _features.Add(feature);
        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            IdentityId = 5,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var result = await _service.GetAsync(EnvironmentId);

        Assert.That(result!.FeatureStates, Is.Empty);
    }

    [Test]
    public async Task WhenTheProjectHasSegments_ThenTheyAreIncludedInTheProjectResponse()
    {
        _projects[0].Segments.Add(new Segment { Id = 1, Name = "Beta Users", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow });

        var result = await _service.GetAsync(EnvironmentId);

        Assert.That(result!.Project.Segments, Has.Count.EqualTo(1));
        Assert.That(result.Project.Segments[0].Name, Is.EqualTo("Beta Users"));
    }
}
