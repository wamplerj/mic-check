using MicCheck.Api.Features;
using MicCheck.Api.Segments;

namespace MicCheck.Api.Environments;

public record EnvironmentDocumentResponse(
    int Id,
    string ApiKey,
    IReadOnlyList<FlagResponse> FeatureStates,
    EnvironmentProjectResponse Project);

public record EnvironmentProjectResponse(
    int Id,
    string Name,
    IReadOnlyList<SegmentDocumentResponse> Segments);

public record SegmentDocumentResponse(
    int Id,
    string Name,
    IReadOnlyList<SegmentRuleDocumentResponse> Rules)
{
    public static SegmentDocumentResponse From(Segment segment) => new(
        segment.Id,
        segment.Name,
        segment.Rules
            .Where(r => r.ParentRuleId == null)
            .Select(SegmentRuleDocumentResponse.From)
            .ToList());
}

public record SegmentRuleDocumentResponse(
    int Id,
    string Type,
    IReadOnlyList<SegmentConditionDocumentResponse> Conditions,
    IReadOnlyList<SegmentRuleDocumentResponse> Rules)
{
    public static SegmentRuleDocumentResponse From(SegmentRule rule) => new(
        rule.Id,
        rule.Type.ToString().ToUpperInvariant(),
        rule.Conditions.Select(SegmentConditionDocumentResponse.From).ToList(),
        rule.ChildRules.Select(From).ToList());
}

public record SegmentConditionDocumentResponse(string Property, string Operator, string Value)
{
    public static SegmentConditionDocumentResponse From(SegmentCondition condition) => new(
        condition.Property,
        condition.Operator.ToString(),
        condition.Value);
}
