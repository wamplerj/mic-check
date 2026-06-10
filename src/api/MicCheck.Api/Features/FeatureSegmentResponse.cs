namespace MicCheck.Api.Features;

public record FeatureSegmentResponse(
    int Id,
    int FeatureId,
    int SegmentId,
    string SegmentName,
    int EnvironmentId,
    int Priority,
    bool? Enabled,
    string? Value
);
