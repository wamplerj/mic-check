using MicCheck.Api.Identities;
using MicCheck.Api.Segments;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Segments;

[TestFixture]
public class SegmentEvaluatorTests
{
    private SegmentEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp() => _evaluator = new SegmentEvaluator();

    private static Segment BuildSegment(SegmentRuleType ruleType, params SegmentCondition[] conditions)
    {
        var segment = new Segment { Name = "Test", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        var rule = new SegmentRule { SegmentId = segment.Id, Type = ruleType };
        foreach (var condition in conditions)
            rule.Conditions.Add(condition);
        segment.Rules.Add(rule);
        return segment;
    }

    private static SegmentCondition Condition(string property, SegmentConditionOperator op, string value) =>
        new() { Property = property, Operator = op, Value = value };

    private static IdentityTrait Trait(string key, string value) =>
        new() { Key = key, Value = value, IdentityId = 1 };

    [Test]
    public void WhenTraitEqualsConditionValue_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("plan", SegmentConditionOperator.Equal, "premium"));
        var traits = new[] { Trait("plan", "premium") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenTraitDoesNotEqualConditionValue_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("plan", SegmentConditionOperator.Equal, "premium"));
        var traits = new[] { Trait("plan", "free") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenNotEqualOperatorAndValuesAreDifferent_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("plan", SegmentConditionOperator.NotEqual, "premium"));
        var traits = new[] { Trait("plan", "free") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenGreaterThanConditionIsSatisfied_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("age", SegmentConditionOperator.GreaterThan, "18"));
        var traits = new[] { Trait("age", "25") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenGreaterThanConditionIsNotSatisfied_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("age", SegmentConditionOperator.GreaterThan, "18"));
        var traits = new[] { Trait("age", "16") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenGreaterThanOrEqualConditionIsExactMatch_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("age", SegmentConditionOperator.GreaterThanOrEqual, "18"));
        var traits = new[] { Trait("age", "18") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenLessThanConditionIsSatisfied_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("age", SegmentConditionOperator.LessThan, "18"));
        var traits = new[] { Trait("age", "16") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenLessThanOrEqualConditionIsExactMatch_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("age", SegmentConditionOperator.LessThanOrEqual, "18"));
        var traits = new[] { Trait("age", "18") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenContainsConditionIsSatisfied_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("email", SegmentConditionOperator.Contains, "@example.com"));
        var traits = new[] { Trait("email", "alice@example.com") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenNotContainsConditionIsSatisfied_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("email", SegmentConditionOperator.NotContains, "@test.com"));
        var traits = new[] { Trait("email", "alice@example.com") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenRegexConditionMatchesTraitValue_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("email", SegmentConditionOperator.Regex, @"^[^@]+@example\.com$"));
        var traits = new[] { Trait("email", "alice@example.com") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenRegexConditionDoesNotMatchTraitValue_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("email", SegmentConditionOperator.Regex, @"^[^@]+@example\.com$"));
        var traits = new[] { Trait("email", "alice@other.com") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenIsSetConditionAndTraitExists_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("plan", SegmentConditionOperator.IsSet, ""));
        var traits = new[] { Trait("plan", "premium") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenIsSetConditionAndTraitDoesNotExist_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("plan", SegmentConditionOperator.IsSet, ""));

        Assert.That(_evaluator.Evaluate(segment, [], "user1"), Is.False);
    }

    [Test]
    public void WhenIsNotSetConditionAndTraitDoesNotExist_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("plan", SegmentConditionOperator.IsNotSet, ""));

        Assert.That(_evaluator.Evaluate(segment, [], "user1"), Is.True);
    }

    [Test]
    public void WhenInConditionAndValueIsInList_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("country", SegmentConditionOperator.In, "US,UK,CA"));
        var traits = new[] { Trait("country", "UK") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenNotInConditionAndValueIsNotInList_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("country", SegmentConditionOperator.NotIn, "US,UK,CA"));
        var traits = new[] { Trait("country", "AU") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenIsTrueConditionAndValueIsTrue_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("beta", SegmentConditionOperator.IsTrue, ""));
        var traits = new[] { Trait("beta", "true") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenIsFalseConditionAndValueIsFalse_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("beta", SegmentConditionOperator.IsFalse, ""));
        var traits = new[] { Trait("beta", "false") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenModuloConditionIsSatisfied_ThenSegmentMatches()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("userId", SegmentConditionOperator.ModuloValueDivisorRemainder, "3|0"));
        var traits = new[] { Trait("userId", "9") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenModuloConditionIsNotSatisfied_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("userId", SegmentConditionOperator.ModuloValueDivisorRemainder, "3|0"));
        var traits = new[] { Trait("userId", "7") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenPercentageSplitIsDeterministic_ThenSameInputProducesSameResult()
    {
        var segment = new Segment { Id = 1, Name = "Test", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        var rule = new SegmentRule { SegmentId = 1, Type = SegmentRuleType.All };
        rule.Conditions.Add(Condition("", SegmentConditionOperator.PercentageSplit, "50"));
        segment.Rules.Add(rule);

        var result1 = _evaluator.Evaluate(segment, [], "user-abc");
        var result2 = _evaluator.Evaluate(segment, [], "user-abc");

        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void WhenPercentageSplitIs100_ThenAllIdentitiesAreIncluded()
    {
        var segment = new Segment { Id = 42, Name = "Test", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        var rule = new SegmentRule { SegmentId = 42, Type = SegmentRuleType.All };
        rule.Conditions.Add(Condition("", SegmentConditionOperator.PercentageSplit, "100"));
        segment.Rules.Add(rule);

        Assert.That(_evaluator.Evaluate(segment, [], "any-user"), Is.True);
        Assert.That(_evaluator.Evaluate(segment, [], "another-user"), Is.True);
    }

    [Test]
    public void WhenPercentageSplitIs0_ThenNoIdentitiesAreIncluded()
    {
        var segment = new Segment { Id = 99, Name = "Test", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        var rule = new SegmentRule { SegmentId = 99, Type = SegmentRuleType.All };
        rule.Conditions.Add(Condition("", SegmentConditionOperator.PercentageSplit, "0"));
        segment.Rules.Add(rule);

        Assert.That(_evaluator.Evaluate(segment, [], "any-user"), Is.False);
        Assert.That(_evaluator.Evaluate(segment, [], "another-user"), Is.False);
    }

    [Test]
    public void WhenModuloConditionValueIsMalformed_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("userId", SegmentConditionOperator.ModuloValueDivisorRemainder, "not-a-modulo-spec"));
        var traits = new[] { Trait("userId", "9") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenModuloDivisorIsZero_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("userId", SegmentConditionOperator.ModuloValueDivisorRemainder, "0|0"));
        var traits = new[] { Trait("userId", "9") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenPercentageSplitValueIsNotANumber_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.All,
            Condition("", SegmentConditionOperator.PercentageSplit, "not-a-number"));

        Assert.That(_evaluator.Evaluate(segment, [], "any-user"), Is.False);
    }

    [Test]
    public void WhenARuleHasAChildRuleThatFails_ThenTheParentRuleDoesNotMatchEvenIfItsOwnConditionsPass()
    {
        var segment = new Segment { Name = "Test", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        var parent = new SegmentRule { Type = SegmentRuleType.All };
        parent.Conditions.Add(Condition("plan", SegmentConditionOperator.Equal, "premium"));
        var child = new SegmentRule { Type = SegmentRuleType.All };
        child.Conditions.Add(Condition("country", SegmentConditionOperator.Equal, "US"));
        parent.ChildRules.Add(child);
        segment.Rules.Add(parent);

        var traits = new[] { Trait("plan", "premium"), Trait("country", "UK") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenRuleTypeIsAny_ThenAtLeastOneConditionMustMatch()
    {
        var segment = BuildSegment(SegmentRuleType.Any,
            Condition("plan", SegmentConditionOperator.Equal, "premium"),
            Condition("plan", SegmentConditionOperator.Equal, "enterprise"));
        var traits = new[] { Trait("plan", "enterprise") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenRuleTypeIsNone_ThenNoConditionMustMatch()
    {
        var segment = BuildSegment(SegmentRuleType.None,
            Condition("plan", SegmentConditionOperator.Equal, "premium"),
            Condition("plan", SegmentConditionOperator.Equal, "enterprise"));
        var traits = new[] { Trait("plan", "free") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.True);
    }

    [Test]
    public void WhenRuleTypeIsNoneAndOneConditionMatches_ThenSegmentDoesNotMatch()
    {
        var segment = BuildSegment(SegmentRuleType.None,
            Condition("plan", SegmentConditionOperator.Equal, "premium"));
        var traits = new[] { Trait("plan", "premium") };

        Assert.That(_evaluator.Evaluate(segment, traits), Is.False);
    }

    [Test]
    public void WhenAllTopLevelRulesMustPass_ThenSegmentOnlyMatchesWhenAllPass()
    {
        var segment = new Segment { Name = "Test", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };

        var rule1 = new SegmentRule { Type = SegmentRuleType.All };
        rule1.Conditions.Add(Condition("plan", SegmentConditionOperator.Equal, "premium"));

        var rule2 = new SegmentRule { Type = SegmentRuleType.All };
        rule2.Conditions.Add(Condition("country", SegmentConditionOperator.Equal, "US"));

        segment.Rules.Add(rule1);
        segment.Rules.Add(rule2);

        var matchingTraits = new[] { Trait("plan", "premium"), Trait("country", "US") };
        var nonMatchingTraits = new[] { Trait("plan", "premium"), Trait("country", "UK") };

        Assert.That(_evaluator.Evaluate(segment, matchingTraits), Is.True);
        Assert.That(_evaluator.Evaluate(segment, nonMatchingTraits), Is.False);
    }
}
