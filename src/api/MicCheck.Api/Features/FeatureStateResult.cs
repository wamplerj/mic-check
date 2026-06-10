namespace MicCheck.Api.Features;

public class FeatureStateResult
{
    public required Feature Feature { get; init; }
    public bool Enabled { get; init; }
    public string? Value { get; init; }
}
