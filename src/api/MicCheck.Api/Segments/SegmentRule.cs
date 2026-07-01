namespace MicCheck.Api.Segments;

public class SegmentRule
{
    public int Id { get; init; }
    public int SegmentId { get; init; }
    public int? ParentRuleId { get; set; }
    public SegmentRuleType Type { get; set; }
    public ICollection<SegmentCondition> Conditions { get; init; } = [];
    public ICollection<SegmentRule> ChildRules { get; init; } = [];
}

public enum SegmentRuleType { All, Any, None }
