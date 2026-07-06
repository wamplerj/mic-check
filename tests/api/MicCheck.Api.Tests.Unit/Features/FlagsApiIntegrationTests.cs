using System.Security.Claims;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
using MicCheck.Api.Features.Usage;
using MicCheck.Api.Identities;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FlagsApiIntegrationTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AppEnvironment> _environments = null!;
    private List<Project> _projects = null!;
    private List<Feature> _features = null!;
    private List<FeatureState> _featureStates = null!;
    private List<Identity> _identities = null!;
    private AppEnvironment _environment = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _projects = [new Project { Id = 1, Name = "Test Project", OrganizationId = 1, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSet(c => c.Projects, _projects);

        _environment = new AppEnvironment
        {
            Id = 1,
            Name = "Test Env",
            ApiKey = "test-env-key",
            ProjectId = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _environments = [_environment];
        var environmentsSet = MockDbSetFactory.Create(_environments);
        environmentsSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken _) => _environments.FirstOrDefault(e => e.Id == (int)keys[0]));
        _db.Setup(c => c.Environments).Returns(environmentsSet.Object);

        _features = [new Feature { Id = 1, Name = "dark_mode", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow }];
        _db.SetupDbSetWithGeneratedIds(c => c.Features, _features);

        _featureStates = [
            new FeatureState { FeatureId = 1, EnvironmentId = 1, Enabled = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        ];
        _db.SetupDbSet(c => c.FeatureStates, _featureStates);

        _identities = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Identities, _identities);
        _db.SetupDbSet(c => c.IdentityTraits, []);
        _db.SetupDbSetWithGeneratedIds(c => c.Segments, []);
        _db.SetupDbSet(c => c.SegmentRules, []);
        _db.SetupDbSet(c => c.SegmentConditions, []);
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureSegments, []);
    }

    private static ControllerBase WithEnvironmentClaim(ControllerBase controller, int environmentId)
    {
        var identity = new ClaimsIdentity([new Claim("EnvironmentId", environmentId.ToString())], "EnvironmentKey");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private FeatureEvaluationService CreateEvaluationService() =>
        new(_db.Object, new SegmentEvaluator(), new FlagCache(new MemoryCache(new MemoryCacheOptions())), CreateUsageMetrics());

    private static FeatureUsageMetrics CreateUsageMetrics()
    {
        var meterFactory = new Mock<System.Diagnostics.Metrics.IMeterFactory>();
        meterFactory.Setup(f => f.Create(It.IsAny<System.Diagnostics.Metrics.MeterOptions>()))
            .Returns(new System.Diagnostics.Metrics.Meter("test"));
        return new FeatureUsageMetrics(meterFactory.Object);
    }

    [Test]
    public async Task WhenEnvironmentKeyIsValid_ThenFlagsAreReturned()
    {
        var controller = (FlagsController)WithEnvironmentClaim(
            new FlagsController(CreateEvaluationService()), _environment.Id);

        var result = await controller.GetAll(CancellationToken.None);

        var flags = (result.Result as OkObjectResult)?.Value as IReadOnlyList<FlagResponse>;
        Assert.That(flags, Has.Count.EqualTo(1));
        Assert.That(flags![0].Feature.Name, Is.EqualTo("dark_mode"));
        Assert.That(flags[0].Enabled, Is.True);
    }

    [Test]
    public async Task WhenIdentifyingUserWithValidKey_ThenFlagsAndTraitsAreReturned()
    {
        var controller = (IdentitiesController)WithEnvironmentClaim(
            new IdentitiesController(CreateEvaluationService(), new IdentityResolutionService(_db.Object)),
            _environment.Id);

        var result = await controller.Identify(
            new IdentityRequest("user-123", [new TraitInput("plan", "premium")]), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task WhenGettingEnvironmentDocument_ThenDocumentIsReturned()
    {
        var controller = (EnvironmentDocumentController)WithEnvironmentClaim(
            new EnvironmentDocumentController(new EnvironmentDocumentService(_db.Object)),
            _environment.Id);

        var result = await controller.Get(CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }
}
