using NUnit.Framework;
using MicCheck.Api.Features;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureTests
{
    [Test]
    public void WhenAFeatureIsCreated_ThenCollectionsAreInitializedEmpty()
    {
        var feature = new Feature { Name = "dark_mode", ProjectId = 1 };

        Assert.That(feature.FeatureStates, Is.Empty);
        Assert.That(feature.Tags, Is.Empty);
    }

    [Test]
    public void WhenAFeatureIsCreated_ThenTypeDefaultsToStandard()
    {
        var feature = new Feature { Name = "dark_mode", ProjectId = 1 };

        Assert.That(feature.Type, Is.EqualTo(FeatureType.Standard));
    }

    [Test]
    public void WhenAFeatureIsCreated_ThenDefaultEnabledIsFalse()
    {
        var feature = new Feature { Name = "dark_mode", ProjectId = 1 };

        Assert.That(feature.DefaultEnabled, Is.False);
    }

    [Test]
    public void WhenAFeatureTypeIsSetToMultiVariate_ThenThePropertyReflectsTheChange()
    {
        var feature = new Feature { Name = "experiment", ProjectId = 1, Type = FeatureType.MultiVariate };

        Assert.That(feature.Type, Is.EqualTo(FeatureType.MultiVariate));
    }
}

[TestFixture]
public class FeatureStateTests
{
    [Test]
    public void WhenAFeatureStateIsCreated_ThenEnabledDefaultsToFalse()
    {
        var state = new FeatureState { FeatureId = 1, EnvironmentId = 1 };

        Assert.That(state.Enabled, Is.False);
    }

    [Test]
    public void WhenAFeatureStateIdentityIdIsNull_ThenItRepresentsAnEnvironmentLevelState()
    {
        var state = new FeatureState { FeatureId = 1, EnvironmentId = 1 };

        Assert.That(state.IdentityId, Is.Null);
    }

    [Test]
    public void WhenAFeatureStateFeatureSegmentIdIsNull_ThenItIsNotASegmentOverride()
    {
        var state = new FeatureState { FeatureId = 1, EnvironmentId = 1 };

        Assert.That(state.FeatureSegmentId, Is.Null);
    }
}

[TestFixture]
public class FeatureStateResultTests
{
    [Test]
    public void WhenAFeatureStateResultIsCreated_ThenFeatureAndEnabledAreSet()
    {
        var feature = new Feature { Name = "dark_mode", ProjectId = 1 };
        var result = new FeatureStateResult { Feature = feature, Enabled = true };

        Assert.That(result.Feature, Is.SameAs(feature));
        Assert.That(result.Enabled, Is.True);
        Assert.That(result.Value, Is.Null);
    }
}

[TestFixture]
public class FeatureSegmentTests
{
    [Test]
    public void WhenAFeatureSegmentIsCreated_ThenFeatureStateIsNull()
    {
        var segment = new FeatureSegment { FeatureId = 1, SegmentId = 2, EnvironmentId = 3 };

        Assert.That(segment.FeatureState, Is.Null);
    }
}

[TestFixture]
public class TagTests
{
    [Test]
    public void WhenATagIsCreated_ThenLabelColorAndProjectIdAreSet()
    {
        var tag = new Tag { Label = "Beta", Color = "#FF0000", ProjectId = 1 };

        Assert.That(tag.Label, Is.EqualTo("Beta"));
        Assert.That(tag.Color, Is.EqualTo("#FF0000"));
        Assert.That(tag.ProjectId, Is.EqualTo(1));
    }
}
