namespace MicCheck.Api.Features;

public record CreateFeatureSegmentRequest(int SegmentId, int Priority, bool Enabled, string? Value);

public record UpdateFeatureSegmentRequest(int Priority, bool Enabled, string? Value);
