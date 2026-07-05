using MicCheck.Api.Segments;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Segments;

[TestFixture]
public class SegmentResponseTests
{
    [Test]
    public void WhenMappingASegmentWithNoRules_ThenTheResponseHasAnEmptyRulesList()
    {
        var segment = new Segment { Id = 1, Name = "Premium Users", ProjectId = 5, CreatedAt = DateTimeOffset.UtcNow };

        var response = SegmentResponse.From(segment);

        Assert.That(response.Id, Is.EqualTo(segment.Id));
        Assert.That(response.Name, Is.EqualTo("Premium Users"));
        Assert.That(response.ProjectId, Is.EqualTo(5));
        Assert.That(response.Rules, Is.Empty);
    }

    [Test]
    public void WhenMappingASegmentWithTopLevelRules_ThenOnlyRulesWithoutAParentAreAtTheTopLevel()
    {
        var segment = new Segment { Id = 1, Name = "Test", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        var topLevelRule = new SegmentRule { Id = 1, SegmentId = 1, Type = SegmentRuleType.All };
        var childRule = new SegmentRule { Id = 2, SegmentId = 1, ParentRuleId = 1, Type = SegmentRuleType.Any };
        segment.Rules.Add(topLevelRule);
        segment.Rules.Add(childRule);

        var response = SegmentResponse.From(segment);

        Assert.That(response.Rules, Has.Count.EqualTo(1));
        Assert.That(response.Rules[0].Id, Is.EqualTo(topLevelRule.Id));
    }

    [Test]
    public void WhenMappingARuleWithChildRules_ThenChildRulesAreNestedUnderTheParent()
    {
        var parent = new SegmentRule { Id = 1, SegmentId = 1, Type = SegmentRuleType.All };
        var child = new SegmentRule { Id = 2, SegmentId = 1, ParentRuleId = 1, Type = SegmentRuleType.Any };
        var allRules = new List<SegmentRule> { parent, child };

        var response = SegmentRuleResponse.From(parent, allRules);

        Assert.That(response.Type, Is.EqualTo(nameof(SegmentRuleType.All)));
        Assert.That(response.ChildRules, Has.Count.EqualTo(1));
        Assert.That(response.ChildRules[0].Id, Is.EqualTo(child.Id));
    }

    [Test]
    public void WhenMappingARuleWithConditions_ThenEachConditionIsMapped()
    {
        var rule = new SegmentRule { Id = 1, SegmentId = 1, Type = SegmentRuleType.All };
        rule.Conditions.Add(new SegmentCondition { Id = 1, RuleId = 1, Property = "plan", Operator = SegmentConditionOperator.Equal, Value = "premium" });

        var response = SegmentRuleResponse.From(rule, [rule]);

        Assert.That(response.Conditions, Has.Count.EqualTo(1));
        Assert.That(response.Conditions[0].Property, Is.EqualTo("plan"));
    }

    [Test]
    public void WhenMappingACondition_ThenIdPropertyOperatorAndValueAreCopied()
    {
        var condition = new SegmentCondition { Id = 7, RuleId = 1, Property = "plan", Operator = SegmentConditionOperator.Contains, Value = "premium" };

        var response = SegmentConditionResponse.From(condition);

        Assert.That(response.Id, Is.EqualTo(7));
        Assert.That(response.Property, Is.EqualTo("plan"));
        Assert.That(response.Operator, Is.EqualTo(nameof(SegmentConditionOperator.Contains)));
        Assert.That(response.Value, Is.EqualTo("premium"));
    }

    [Test]
    public void WhenCreatingASegmentSummaryResponse_ThenIdAndNameAreSet()
    {
        var response = new SegmentSummaryResponse(1, "Premium Users");

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Name, Is.EqualTo("Premium Users"));
    }
}
