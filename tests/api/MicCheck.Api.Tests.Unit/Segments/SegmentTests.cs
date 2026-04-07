using NUnit.Framework;
using MicCheck.Api.Segments;

namespace MicCheck.Api.Tests.Unit.Segments;

[TestFixture]
public class SegmentTests
{
    [Test]
    public void WhenASegmentIsCreated_ThenRulesCollectionIsInitializedEmpty()
    {
        var segment = new Segment { Name = "Premium Users", ProjectId = 1 };

        Assert.That(segment.Rules, Is.Empty);
    }

    [Test]
    public void WhenASegmentIsCreated_ThenNameAndProjectIdAreSet()
    {
        var segment = new Segment { Name = "Beta Testers", ProjectId = 5 };

        Assert.That(segment.Name, Is.EqualTo("Beta Testers"));
        Assert.That(segment.ProjectId, Is.EqualTo(5));
    }
}

[TestFixture]
public class SegmentRuleTests
{
    [Test]
    public void WhenASegmentRuleIsCreated_ThenConditionsAndChildRulesAreInitializedEmpty()
    {
        var rule = new SegmentRule { SegmentId = 1 };

        Assert.That(rule.Conditions, Is.Empty);
        Assert.That(rule.ChildRules, Is.Empty);
    }

    [Test]
    public void WhenASegmentRuleIsCreated_ThenParentRuleIdIsNull()
    {
        var rule = new SegmentRule { SegmentId = 1 };

        Assert.That(rule.ParentRuleId, Is.Null);
    }

    [TestCase(SegmentRuleType.All)]
    [TestCase(SegmentRuleType.Any)]
    [TestCase(SegmentRuleType.None)]
    public void WhenASegmentRuleTypeIsSet_ThenTheTypeIsStored(SegmentRuleType type)
    {
        var rule = new SegmentRule { SegmentId = 1, Type = type };

        Assert.That(rule.Type, Is.EqualTo(type));
    }
}

[TestFixture]
public class SegmentConditionTests
{
    [Test]
    public void WhenASegmentConditionIsCreated_ThenPropertyAndValueAreSet()
    {
        var condition = new SegmentCondition
        {
            RuleId = 1,
            Property = "plan",
            Operator = SegmentConditionOperator.Equal,
            Value = "premium"
        };

        Assert.That(condition.Property, Is.EqualTo("plan"));
        Assert.That(condition.Value, Is.EqualTo("premium"));
        Assert.That(condition.Operator, Is.EqualTo(SegmentConditionOperator.Equal));
    }

    [Test]
    public void WhenAllSegmentConditionOperatorsAreChecked_ThenAllExpectedValuesExist()
    {
        var operators = Enum.GetValues<SegmentConditionOperator>();

        Assert.That(operators, Contains.Item(SegmentConditionOperator.Equal));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.NotEqual));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.GreaterThan));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.GreaterThanOrEqual));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.LessThan));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.LessThanOrEqual));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.Contains));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.NotContains));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.Regex));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.IsSet));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.IsNotSet));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.In));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.NotIn));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.PercentageSplit));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.ModuloValueDivisorRemainder));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.IsTrue));
        Assert.That(operators, Contains.Item(SegmentConditionOperator.IsFalse));
    }
}
