using System.Text.Json;
using MicCheck.Api.Identities;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Identities;

[TestFixture]
public class TraitInputTests
{
    private static JsonElement ParseElement(string json) => JsonDocument.Parse(json).RootElement;

    [Test]
    public void WhenTraitValueIsAJsonString_ThenGetStringValueReturnsTheString()
    {
        var trait = new TraitInput("plan", ParseElement("\"premium\""));

        Assert.That(trait.GetStringValue(), Is.EqualTo("premium"));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.String));
    }

    [Test]
    public void WhenTraitValueIsJsonTrue_ThenGetStringValueReturnsTrueAndTypeIsBoolean()
    {
        var trait = new TraitInput("beta", ParseElement("true"));

        Assert.That(trait.GetStringValue(), Is.EqualTo("true"));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Boolean));
    }

    [Test]
    public void WhenTraitValueIsJsonFalse_ThenGetStringValueReturnsFalseAndTypeIsBoolean()
    {
        var trait = new TraitInput("beta", ParseElement("false"));

        Assert.That(trait.GetStringValue(), Is.EqualTo("false"));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Boolean));
    }

    [Test]
    public void WhenTraitValueIsJsonNull_ThenGetStringValueReturnsEmptyString()
    {
        var trait = new TraitInput("plan", ParseElement("null"));

        Assert.That(trait.GetStringValue(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void WhenTraitValueIsAJsonInteger_ThenTypeIsInteger()
    {
        var trait = new TraitInput("age", ParseElement("42"));

        Assert.That(trait.GetStringValue(), Is.EqualTo("42"));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Integer));
    }

    [Test]
    public void WhenTraitValueIsAJsonFloat_ThenTypeIsFloat()
    {
        var trait = new TraitInput("score", ParseElement("4.5"));

        Assert.That(trait.GetStringValue(), Is.EqualTo("4.5"));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Float));
    }

    [Test]
    public void WhenTraitValueIsAJsonArray_ThenGetStringValueFallsBackToToString()
    {
        var element = ParseElement("[1,2,3]");
        var trait = new TraitInput("list", element);

        Assert.That(trait.GetStringValue(), Is.EqualTo(element.ToString()));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.String));
    }

    [Test]
    public void WhenTraitValueIsNull_ThenGetStringValueReturnsEmptyStringAndTypeIsString()
    {
        var trait = new TraitInput("plan", null);

        Assert.That(trait.GetStringValue(), Is.EqualTo(string.Empty));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.String));
    }

    [Test]
    public void WhenTraitValueIsAPlainBool_ThenTypeIsBoolean()
    {
        var trait = new TraitInput("beta", true);

        Assert.That(trait.GetStringValue(), Is.EqualTo("True"));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Boolean));
    }

    [Test]
    public void WhenTraitValueIsAPlainInt_ThenTypeIsInteger()
    {
        var trait = new TraitInput("age", 42);

        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Integer));
    }

    [Test]
    public void WhenTraitValueIsAPlainLong_ThenTypeIsInteger()
    {
        var trait = new TraitInput("age", 42L);

        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Integer));
    }

    [Test]
    public void WhenTraitValueIsAPlainDouble_ThenTypeIsFloat()
    {
        var trait = new TraitInput("score", 4.5d);

        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.Float));
    }

    [Test]
    public void WhenTraitValueIsAPlainString_ThenTypeIsString()
    {
        var trait = new TraitInput("plan", "premium");

        Assert.That(trait.GetStringValue(), Is.EqualTo("premium"));
        Assert.That(trait.GetValueType(), Is.EqualTo(TraitValueType.String));
    }
}
