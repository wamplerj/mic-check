namespace MicCheck.Api.Segments;

public record SegmentResponse(
    int Id,
    string Name,
    int ProjectId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SegmentRuleResponse> Rules
)
{
    public static SegmentResponse From(Segment segment) => new(
        segment.Id,
        segment.Name,
        segment.ProjectId,
        segment.CreatedAt,
        segment.Rules
            .Where(r => r.ParentRuleId == null)
            .Select(r => SegmentRuleResponse.From(r, segment.Rules.ToList()))
            .ToList());
}

public record SegmentRuleResponse(
    int Id,
    string Type,
    IReadOnlyList<SegmentConditionResponse> Conditions,
    IReadOnlyList<SegmentRuleResponse> ChildRules
)
{
    public static SegmentRuleResponse From(SegmentRule rule, List<SegmentRule> allRules) => new(
        rule.Id,
        rule.Type.ToString(),
        rule.Conditions.Select(SegmentConditionResponse.From).ToList(),
        allRules
            .Where(r => r.ParentRuleId == rule.Id)
            .Select(r => SegmentRuleResponse.From(r, allRules))
            .ToList());
}

public record SegmentConditionResponse(
    int Id,
    string Property,
    string Operator,
    string Value
)
{
    public static SegmentConditionResponse From(SegmentCondition condition) => new(
        condition.Id,
        condition.Property,
        condition.Operator.ToString(),
        condition.Value);
}
