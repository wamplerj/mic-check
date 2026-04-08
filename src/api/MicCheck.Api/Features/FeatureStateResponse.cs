namespace MicCheck.Api.Features;

public record FeatureStateResponse(
    int Id,
    int FeatureId,
    int EnvironmentId,
    int? IdentityId,
    int? FeatureSegmentId,
    bool Enabled,
    string? Value,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
)
{
    public static FeatureStateResponse From(FeatureState state) => new(
        state.Id,
        state.FeatureId,
        state.EnvironmentId,
        state.IdentityId,
        state.FeatureSegmentId,
        state.Enabled,
        state.Value,
        state.CreatedAt,
        state.UpdatedAt);
}
