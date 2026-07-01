using NUnit.Framework;
using MicCheck.Api.Identities;

namespace MicCheck.Api.Tests.Unit.Identities;

[TestFixture]
public class IdentityTests
{
    [Test]
    public void WhenAnIdentityIsCreated_ThenCollectionsAreInitializedEmpty()
    {
        var identity = new Identity { Identifier = "user-123", EnvironmentId = 1 };

        Assert.That(identity.Traits, Is.Empty);
        Assert.That(identity.FeatureStateOverrides, Is.Empty);
    }

    [Test]
    public void WhenAnIdentityIsCreated_ThenIdentifierAndEnvironmentIdAreSet()
    {
        var identity = new Identity { Identifier = "user-123", EnvironmentId = 5 };

        Assert.That(identity.Identifier, Is.EqualTo("user-123"));
        Assert.That(identity.EnvironmentId, Is.EqualTo(5));
    }
}

[TestFixture]
public class IdentityTraitTests
{
    [Test]
    public void WhenAnIdentityTraitIsCreated_ThenKeyAndValueAreSet()
    {
        var trait = new IdentityTrait { IdentityId = 1, Key = "plan", Value = "premium" };

        Assert.That(trait.Key, Is.EqualTo("plan"));
        Assert.That(trait.Value, Is.EqualTo("premium"));
    }

    [Test]
    public void WhenAnIdentityTraitValueTypeIsSet_ThenTheTypeIsStored()
    {
        var trait = new IdentityTrait
        {
            IdentityId = 1,
            Key = "age",
            Value = "30",
            ValueType = TraitValueType.Integer
        };

        Assert.That(trait.ValueType, Is.EqualTo(TraitValueType.Integer));
    }

    [Test]
    public void WhenAllTraitValueTypesAreChecked_ThenAllExpectedValuesExist()
    {
        var types = Enum.GetValues<TraitValueType>();

        Assert.That(types, Contains.Item(TraitValueType.String));
        Assert.That(types, Contains.Item(TraitValueType.Integer));
        Assert.That(types, Contains.Item(TraitValueType.Float));
        Assert.That(types, Contains.Item(TraitValueType.Boolean));
    }
}
