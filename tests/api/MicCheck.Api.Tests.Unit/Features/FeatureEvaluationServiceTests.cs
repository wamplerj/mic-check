using System.Diagnostics.Metrics;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Identities;
using MicCheck.Api.Segments;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureEvaluationServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AppEnvironment> _environments = null!;
    private List<Feature> _features = null!;
    private List<FeatureState> _featureStates = null!;
    private List<Identity> _identities = null!;
    private List<Segment> _segments = null!;
    private List<FeatureSegment> _featureSegments = null!;
    private FeatureEvaluationService _service = null!;
    private FeatureUsageMetrics _usageMetrics = null!;
    private const int EnvironmentId = 1;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _environments = [new AppEnvironment { Id = EnvironmentId, Name = "Production", ApiKey = "env-key-test", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }];
        var environmentsSet = MockDbSetFactory.Create(_environments);
        environmentsSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken _) => _environments.FirstOrDefault(e => e.Id == (int)keys[0]));
        _db.Setup(c => c.Environments).Returns(environmentsSet.Object);

        _features = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Features, _features);
        _featureStates = [];
        _db.SetupDbSet(c => c.FeatureStates, _featureStates);
        _identities = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Identities, _identities);
        _db.SetupDbSet(c => c.IdentityTraits, []);
        _segments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.Segments, _segments);
        _db.SetupDbSet(c => c.SegmentRules, []);
        _db.SetupDbSet(c => c.SegmentConditions, []);
        _featureSegments = [];
        _db.SetupDbSetWithGeneratedIds(c => c.FeatureSegments, _featureSegments);

        var meterFactory = new Mock<IMeterFactory>();
        meterFactory.Setup(f => f.Create(It.IsAny<MeterOptions>())).Returns(new Meter("test"));
        _usageMetrics = new FeatureUsageMetrics(meterFactory.Object);

        var cache = new FlagCache(new MemoryCache(new MemoryCacheOptions()));
        _service = new FeatureEvaluationService(_db.Object, new SegmentEvaluator(), cache, _usageMetrics);
    }

    [TearDown]
    public void TearDown() => _usageMetrics.Dispose();

    private Feature AddFeature(string name, bool defaultEnabled = false)
    {
        var feature = new Feature
        {
            Name = name,
            ProjectId = ProjectId,
            CreatedAt = DateTimeOffset.UtcNow,
            DefaultEnabled = defaultEnabled
        };
        _db.Object.Features.Add(feature);
        return feature;
    }

    private FeatureState AddEnvironmentDefault(int featureId, bool enabled, string? value = null)
    {
        var fs = new FeatureState
        {
            FeatureId = featureId,
            EnvironmentId = EnvironmentId,
            Enabled = enabled,
            Value = value,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _featureStates.Add(fs);
        return fs;
    }

    [Test]
    public async Task WhenNoOverridesExist_ThenEnvironmentDefaultIsReturned()
    {
        var feature = AddFeature("dark_mode");
        AddEnvironmentDefault(feature.Id, enabled: true, value: null);

        var results = await _service.EvaluateForEnvironmentAsync(EnvironmentId);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Feature.Name, Is.EqualTo("dark_mode"));
        Assert.That(results[0].Enabled, Is.True);
    }

    [Test]
    public async Task WhenFeatureIsDisabled_ThenResultReflectsThat()
    {
        var feature = AddFeature("beta_feature");
        AddEnvironmentDefault(feature.Id, enabled: false);

        var results = await _service.EvaluateForEnvironmentAsync(EnvironmentId);

        Assert.That(results[0].Enabled, Is.False);
    }

    [Test]
    public async Task WhenIdentityHasNoOverrides_ThenEnvironmentDefaultIsUsed()
    {
        var feature = AddFeature("dark_mode");
        AddEnvironmentDefault(feature.Id, enabled: true);

        var results = await _service.EvaluateForIdentityAsync(EnvironmentId, "user-123");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Enabled, Is.True);
    }

    [Test]
    public async Task WhenIdentityHasOverride_ThenIdentityOverrideTakesPriority()
    {
        var feature = AddFeature("dark_mode");
        AddEnvironmentDefault(feature.Id, enabled: false);

        var identity = new Identity
        {
            Identifier = "user-vip",
            EnvironmentId = EnvironmentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Object.Identities.Add(identity);

        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            IdentityId = identity.Id,
            Enabled = true,
            Value = "identity-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var results = await _service.EvaluateForIdentityAsync(EnvironmentId, "user-vip");

        Assert.That(results[0].Enabled, Is.True);
        Assert.That(results[0].Value, Is.EqualTo("identity-value"));
    }

    [Test]
    public async Task WhenSegmentMatchesAndHasOverride_ThenSegmentOverrideIsUsed()
    {
        var feature = AddFeature("premium_feature");
        AddEnvironmentDefault(feature.Id, enabled: false);

        var segment = new Segment { Name = "Premium Users", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _db.Object.Segments.Add(segment);

        var rule = new SegmentRule { SegmentId = segment.Id, Type = SegmentRuleType.All };
        rule.Conditions.Add(new SegmentCondition
        {
            RuleId = rule.Id,
            Property = "plan",
            Operator = SegmentConditionOperator.Equal,
            Value = "premium"
        });
        segment.Rules.Add(rule);

        var featureSegment = new FeatureSegment
        {
            FeatureId = feature.Id,
            SegmentId = segment.Id,
            EnvironmentId = EnvironmentId,
            Priority = 1
        };
        _db.Object.FeatureSegments.Add(featureSegment);

        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            FeatureSegmentId = featureSegment.Id,
            Enabled = true,
            Value = "segment-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var results = await _service.EvaluateForIdentityAsync(
            EnvironmentId, "user-123",
            [new TraitInput("plan", "premium")]);

        Assert.That(results[0].Enabled, Is.True);
        Assert.That(results[0].Value, Is.EqualTo("segment-value"));
    }

    [Test]
    public async Task WhenSegmentDoesNotMatch_ThenEnvironmentDefaultIsUsed()
    {
        var feature = AddFeature("premium_feature");
        AddEnvironmentDefault(feature.Id, enabled: false, value: "default-value");

        var segment = new Segment { Name = "Premium Users", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _db.Object.Segments.Add(segment);

        var rule = new SegmentRule { SegmentId = segment.Id, Type = SegmentRuleType.All };
        rule.Conditions.Add(new SegmentCondition
        {
            RuleId = rule.Id,
            Property = "plan",
            Operator = SegmentConditionOperator.Equal,
            Value = "premium"
        });
        segment.Rules.Add(rule);

        var featureSegment = new FeatureSegment
        {
            FeatureId = feature.Id,
            SegmentId = segment.Id,
            EnvironmentId = EnvironmentId,
            Priority = 1
        };
        _db.Object.FeatureSegments.Add(featureSegment);

        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            FeatureSegmentId = featureSegment.Id,
            Enabled = true,
            Value = "segment-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var results = await _service.EvaluateForIdentityAsync(
            EnvironmentId, "user-123",
            [new TraitInput("plan", "free")]);

        Assert.That(results[0].Enabled, Is.False);
        Assert.That(results[0].Value, Is.EqualTo("default-value"));
    }

    [Test]
    public async Task WhenIdentityOverrideAndSegmentBothExist_ThenIdentityOverrideTakesPriority()
    {
        var feature = AddFeature("feature_x");
        AddEnvironmentDefault(feature.Id, enabled: false);

        var segment = new Segment { Name = "All Users", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _db.Object.Segments.Add(segment);

        var rule = new SegmentRule { SegmentId = segment.Id, Type = SegmentRuleType.All };
        rule.Conditions.Add(new SegmentCondition
        {
            RuleId = rule.Id,
            Property = "country",
            Operator = SegmentConditionOperator.IsSet,
            Value = ""
        });
        segment.Rules.Add(rule);

        var featureSegment = new FeatureSegment
        {
            FeatureId = feature.Id,
            SegmentId = segment.Id,
            EnvironmentId = EnvironmentId,
            Priority = 1
        };
        _db.Object.FeatureSegments.Add(featureSegment);

        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            FeatureSegmentId = featureSegment.Id,
            Enabled = true,
            Value = "segment-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var identity = new Identity
        {
            Identifier = "user-special",
            EnvironmentId = EnvironmentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Object.Identities.Add(identity);

        identity.Traits.Add(new IdentityTrait
        {
            IdentityId = identity.Id,
            Key = "country",
            Value = "US"
        });

        _featureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            IdentityId = identity.Id,
            Enabled = false,
            Value = "identity-override",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var results = await _service.EvaluateForIdentityAsync(
            EnvironmentId, "user-special",
            [new TraitInput("country", "US")]);

        Assert.That(results[0].Value, Is.EqualTo("identity-override"));
    }
}
