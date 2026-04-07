namespace MicCheck.Api.Segments;

public class SegmentCondition
{
    public int Id { get; init; }
    public int RuleId { get; init; }
    public required string Property { get; set; }
    public SegmentConditionOperator Operator { get; set; }
    public required string Value { get; set; }
}

public enum SegmentConditionOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    NotContains,
    Regex,
    IsSet,
    IsNotSet,
    In,
    NotIn,
    PercentageSplit,
    ModuloValueDivisorRemainder,
    IsTrue,
    IsFalse
}
