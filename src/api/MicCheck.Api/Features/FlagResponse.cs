namespace MicCheck.Api.Features;

public record FlagResponse(int Id, FeatureSummaryResponse Feature, bool Enabled, string? FeatureStateValue)
{
    public static FlagResponse From(FeatureStateResult result) => new(result.Feature.Id, FeatureSummaryResponse.From(result.Feature), result.Enabled, result.Value);
}
