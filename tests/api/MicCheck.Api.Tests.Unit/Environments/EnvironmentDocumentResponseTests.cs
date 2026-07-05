using MicCheck.Api.Environments;
using MicCheck.Api.Segments;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class SegmentDocumentResponseTests
{
    [Test]
    public void WhenMappingASegment_ThenOnlyTopLevelRulesAreIncluded()
    {
        var segment = new Segment { Id = 1, Name = "Beta Users", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        var topLevelRule = new SegmentRule { Id = 1, SegmentId = 1, Type = SegmentRuleType.All };
        var nestedRule = new SegmentRule { Id = 2, SegmentId = 1, ParentRuleId = 1, Type = SegmentRuleType.Any };
        segment.Rules.Add(topLevelRule);
        segment.Rules.Add(nestedRule);

        var response = SegmentDocumentResponse.From(segment);

        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.Name, Is.EqualTo("Beta Users"));
        Assert.That(response.Rules, Has.Count.EqualTo(1));
        Assert.That(response.Rules[0].Id, Is.EqualTo(1));
    }
}

[TestFixture]
public class SegmentRuleDocumentResponseTests
{
    [Test]
    public void WhenMappingARule_ThenTypeIsUpperInvariantAndChildRulesAreIncluded()
    {
        var rule = new SegmentRule { Id = 1, SegmentId = 1, Type = SegmentRuleType.Any };
        var condition = new SegmentCondition { Id = 1, RuleId = 1, Property = "plan", Operator = SegmentConditionOperator.Equal, Value = "pro" };
        var childRule = new SegmentRule { Id = 2, SegmentId = 1, ParentRuleId = 1, Type = SegmentRuleType.All };
        rule.Conditions.Add(condition);
        rule.ChildRules.Add(childRule);

        var response = SegmentRuleDocumentResponse.From(rule);

        Assert.That(response.Type, Is.EqualTo("ANY"));
        Assert.That(response.Conditions, Has.Count.EqualTo(1));
        Assert.That(response.Rules, Has.Count.EqualTo(1));
        Assert.That(response.Rules[0].Id, Is.EqualTo(2));
    }
}

[TestFixture]
public class SegmentConditionDocumentResponseTests
{
    [Test]
    public void WhenMappingACondition_ThenPropertyOperatorAndValueAreCopied()
    {
        var condition = new SegmentCondition { Id = 1, RuleId = 1, Property = "plan", Operator = SegmentConditionOperator.Contains, Value = "pro" };

        var response = SegmentConditionDocumentResponse.From(condition);

        Assert.That(response.Property, Is.EqualTo("plan"));
        Assert.That(response.Operator, Is.EqualTo("Contains"));
        Assert.That(response.Value, Is.EqualTo("pro"));
    }
}
