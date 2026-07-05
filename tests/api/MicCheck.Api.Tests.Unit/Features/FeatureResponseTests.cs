using MicCheck.Api.Features;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FeatureResponseTests
{
    [Test]
    public void WhenMappingAFeatureWithNoTags_ThenTheResponseHasAnEmptyTagsList()
    {
        var feature = new Feature { Id = 1, Name = "dark_mode", ProjectId = 5, Type = FeatureType.Standard, CreatedAt = DateTimeOffset.UtcNow };

        var response = FeatureResponse.From(feature);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Name, Is.EqualTo("dark_mode"));
        Assert.That(response.ProjectId, Is.EqualTo(5));
        Assert.That(response.Tags, Is.Empty);
    }

    [Test]
    public void WhenMappingAFeature_ThenTypeIsUpperInvariant()
    {
        var feature = new Feature { Id = 1, Name = "flag", ProjectId = 1, Type = FeatureType.MultiVariate, CreatedAt = DateTimeOffset.UtcNow };

        var response = FeatureResponse.From(feature);

        Assert.That(response.Type, Is.EqualTo("MULTIVARIATE"));
    }

    [Test]
    public void WhenMappingAFeatureWithTags_ThenEachTagIsMapped()
    {
        var feature = new Feature { Id = 1, Name = "flag", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        feature.Tags.Add(new Tag { Id = 1, Label = "beta", Color = "#FF0000", ProjectId = 1 });

        var response = FeatureResponse.From(feature);

        Assert.That(response.Tags, Has.Count.EqualTo(1));
        Assert.That(response.Tags[0].Label, Is.EqualTo("beta"));
    }
}

[TestFixture]
public class TagResponseTests
{
    [Test]
    public void WhenMappingATag_ThenAllFieldsAreCopied()
    {
        var tag = new Tag { Id = 1, Label = "beta", Color = "#FF0000", ProjectId = 5 };

        var response = TagResponse.From(tag);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Label, Is.EqualTo("beta"));
        Assert.That(response.Color, Is.EqualTo("#FF0000"));
        Assert.That(response.ProjectId, Is.EqualTo(5));
    }
}

[TestFixture]
public class FeatureStateResponseTests
{
    [Test]
    public void WhenMappingAFeatureState_ThenAllFieldsAreCopied()
    {
        var now = DateTimeOffset.UtcNow;
        var state = new FeatureState
        {
            Id = 1,
            FeatureId = 2,
            EnvironmentId = 3,
            Enabled = true,
            Value = "on",
            CreatedAt = now,
            UpdatedAt = now
        };

        var response = FeatureStateResponse.From(state);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.FeatureId, Is.EqualTo(2));
        Assert.That(response.EnvironmentId, Is.EqualTo(3));
        Assert.That(response.Enabled, Is.True);
        Assert.That(response.Value, Is.EqualTo("on"));
    }
}
