using System.Security.Claims;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Features;
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

namespace MicCheck.Api.Tests.Unit.Identities;

[TestFixture]
public class IdentitiesControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<Identity> _identities = null!;
    private IdentitiesController _controller = null!;
    private const int EnvironmentId = 1;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _db.SetupDbSet(c => c.Projects, [
            new Project { Id = ProjectId, Name = "Test Project", OrganizationId = 1, CreatedAt = DateTimeOffset.UtcNow }
        ]);
        var environments = new List<AppEnvironment> {
            new() { Id = EnvironmentId, Name = "Test Env", ApiKey = "test-env-key", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }
        };
        var environmentsSet = MockDbSetFactory.Create(environments);
        environmentsSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken _) => environments.FirstOrDefault(e => e.Id == (int)keys[0]));
        _db.Setup(c => c.Environments).Returns(environmentsSet.Object);
        _db.SetupDbSet(c => c.Features, [
            new Feature { Id = 1, Name = "dark_mode", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }
        ]);
        _db.SetupDbSet(c => c.FeatureStates, [
            new FeatureState { FeatureId = 1, EnvironmentId = EnvironmentId, Enabled = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        ]);
        _identities = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Identities, _identities);
        _db.SetupDbSet(c => c.IdentityTraits, new List<IdentityTrait>());
        _db.SetupDbSetWithGeneratedIds(c => c.Segments, new List<Segment>());
        _db.SetupDbSet(c => c.SegmentRules, new List<SegmentRule>());
        _db.SetupDbSet(c => c.SegmentConditions, new List<SegmentCondition>());
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureSegments, new List<FeatureSegment>());

        var meterFactory = new Mock<System.Diagnostics.Metrics.IMeterFactory>();
        meterFactory.Setup(f => f.Create(It.IsAny<System.Diagnostics.Metrics.MeterOptions>()))
            .Returns(new System.Diagnostics.Metrics.Meter("test"));
        var usageMetrics = new FeatureUsageMetrics(meterFactory.Object);
        var evaluationService = new FeatureEvaluationService(_db.Object, new SegmentEvaluator(), new FlagCache(new MemoryCache(new MemoryCacheOptions())), usageMetrics);
        var resolutionService = new IdentityResolutionService(_db.Object);

        _controller = new IdentitiesController(evaluationService, resolutionService);

        var identity = new ClaimsIdentity([new Claim("EnvironmentId", EnvironmentId.ToString())], "EnvironmentKey");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Test]
    public async Task WhenIdentifierIsMissing_ThenBadRequestIsReturned()
    {
        var result = await _controller.GetByIdentifier("", CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task WhenGettingByIdentifier_ThenFlagsAndTraitsAreReturnedForTheResolvedIdentity()
    {
        var result = await _controller.GetByIdentifier("user-1", CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = (IdentityResponse)ok!.Value!;
        Assert.That(response.Flags, Has.Count.EqualTo(1));
        Assert.That(response.Flags[0].Feature.Name, Is.EqualTo("dark_mode"));
        Assert.That(_identities.Any(i => i.Identifier == "user-1"), Is.True);
    }
}
