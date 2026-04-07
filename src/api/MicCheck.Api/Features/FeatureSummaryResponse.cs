namespace MicCheck.Api.Features;

public record FeatureSummaryResponse(int Id, string Name, string Type)
{
    public static FeatureSummaryResponse From(Feature feature) =>
        new(feature.Id, feature.Name, feature.Type.ToString().ToUpperInvariant());
}
