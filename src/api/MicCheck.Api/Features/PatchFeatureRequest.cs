namespace MicCheck.Api.Features;

public record PatchFeatureRequest(
    string? Name,
    string? Description,
    bool? DefaultEnabled
);
