namespace MicCheck.Api.Features;

public record FeatureResponse(
    int Id,
    string Name,
    string Type,
    string? InitialValue,
    string? Description,
    bool DefaultEnabled,
    int ProjectId,
    DateTimeOffset CreatedAt
)
{
    public static FeatureResponse From(Feature feature) => new(
        feature.Id,
        feature.Name,
        feature.Type.ToString().ToUpperInvariant(),
        feature.InitialValue,
        feature.Description,
        feature.DefaultEnabled,
        feature.ProjectId,
        feature.CreatedAt);
}
