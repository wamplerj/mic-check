using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Identities;
using MicCheck.Api.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureEvaluationServiceTests
{
    private MicCheckDbContext _db = null!;
    private FeatureEvaluationService _service = null!;
    private const int EnvironmentId = 1;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new MicCheckDbContext(options);

        var cache = new FlagCache(new MemoryCache(new MemoryCacheOptions()));
        _service = new FeatureEvaluationService(_db, new SegmentEvaluator(), cache);

        SeedBaseData();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void SeedBaseData()
    {
        _db.Environments.Add(new AppEnvironment
        {
            Id = EnvironmentId,
            Name = "Production",
            ApiKey = "env-key-test",
            ProjectId = ProjectId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    private MicCheck.Api.Features.Feature AddFeature(string name, bool defaultEnabled = false)
    {
        var feature = new MicCheck.Api.Features.Feature
        {
            Name = name,
            ProjectId = ProjectId,
            CreatedAt = DateTimeOffset.UtcNow,
            DefaultEnabled = defaultEnabled
        };
        _db.Features.Add(feature);
        _db.SaveChanges();
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
        _db.FeatureStates.Add(fs);
        _db.SaveChanges();
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
        _db.Identities.Add(identity);
        _db.SaveChanges();

        _db.FeatureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            IdentityId = identity.Id,
            Enabled = true,
            Value = "identity-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

        var results = await _service.EvaluateForIdentityAsync(EnvironmentId, "user-vip");

        Assert.That(results[0].Enabled, Is.True);
        Assert.That(results[0].Value, Is.EqualTo("identity-value"));
    }

    [Test]
    public async Task WhenSegmentMatchesAndHasOverride_ThenSegmentOverrideIsUsed()
    {
        var feature = AddFeature("premium_feature");
        var envDefault = AddEnvironmentDefault(feature.Id, enabled: false);

        var segment = new Segment { Name = "Premium Users", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow };
        _db.Segments.Add(segment);
        _db.SaveChanges();

        var rule = new SegmentRule { SegmentId = segment.Id, Type = SegmentRuleType.All };
        rule.Conditions.Add(new SegmentCondition
        {
            RuleId = rule.Id,
            Property = "plan",
            Operator = SegmentConditionOperator.Equal,
            Value = "premium"
        });
        _db.SegmentRules.Add(rule);
        _db.SaveChanges();

        var featureSegment = new FeatureSegment
        {
            FeatureId = feature.Id,
            SegmentId = segment.Id,
            EnvironmentId = EnvironmentId,
            Priority = 1
        };
        _db.FeatureSegments.Add(featureSegment);
        _db.SaveChanges();

        _db.FeatureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            FeatureSegmentId = featureSegment.Id,
            Enabled = true,
            Value = "segment-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

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
        _db.Segments.Add(segment);
        _db.SaveChanges();

        var rule = new SegmentRule { SegmentId = segment.Id, Type = SegmentRuleType.All };
        rule.Conditions.Add(new SegmentCondition
        {
            RuleId = rule.Id,
            Property = "plan",
            Operator = SegmentConditionOperator.Equal,
            Value = "premium"
        });
        _db.SegmentRules.Add(rule);
        _db.SaveChanges();

        var featureSegment = new FeatureSegment
        {
            FeatureId = feature.Id,
            SegmentId = segment.Id,
            EnvironmentId = EnvironmentId,
            Priority = 1
        };
        _db.FeatureSegments.Add(featureSegment);
        _db.SaveChanges();

        _db.FeatureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            FeatureSegmentId = featureSegment.Id,
            Enabled = true,
            Value = "segment-value",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

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
        _db.Segments.Add(segment);
        _db.SaveChanges();

        var rule = new SegmentRule { SegmentId = segment.Id, Type = SegmentRuleType.All };
        rule.Conditions.Add(new SegmentCondition
        {
            RuleId = rule.Id,
            Property = "country",
            Operator = SegmentConditionOperator.IsSet,
            Value = ""
        });
        _db.SegmentRules.Add(rule);
        _db.SaveChanges();

        var featureSegment = new FeatureSegment
        {
            FeatureId = feature.Id,
            SegmentId = segment.Id,
            EnvironmentId = EnvironmentId,
            Priority = 1
        };
        _db.FeatureSegments.Add(featureSegment);
        _db.SaveChanges();

        _db.FeatureStates.Add(new FeatureState
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
        _db.Identities.Add(identity);
        _db.SaveChanges();

        identity.Traits.Add(new IdentityTrait
        {
            IdentityId = identity.Id,
            Key = "country",
            Value = "US"
        });

        _db.FeatureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = EnvironmentId,
            IdentityId = identity.Id,
            Enabled = false,
            Value = "identity-override",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

        var results = await _service.EvaluateForIdentityAsync(
            EnvironmentId, "user-special",
            [new TraitInput("country", "US")]);

        Assert.That(results[0].Value, Is.EqualTo("identity-override"));
    }
}
