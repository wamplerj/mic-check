namespace MicCheck.Api.Features;

public record UpdateFeatureStateRequest(bool Enabled, string? Value);

public record PatchFeatureStateRequest(bool? Enabled, string? Value);
