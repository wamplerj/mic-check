namespace MicCheck.Api.Features;

public class FeatureSegment
{
    public int Id { get; init; }
    public int FeatureId { get; init; }
    public int SegmentId { get; init; }
    public int EnvironmentId { get; init; }
    public int Priority { get; set; }
    public FeatureState? FeatureState { get; set; }
}
