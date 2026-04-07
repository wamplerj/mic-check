namespace MicCheck.Api.Features;

public class Feature
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public FeatureType Type { get; set; }
    public string? InitialValue { get; set; }
    public string? Description { get; set; }
    public bool DefaultEnabled { get; set; }
    public int ProjectId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<FeatureState> FeatureStates { get; init; } = [];
    public ICollection<Tag> Tags { get; init; } = [];
}

public enum FeatureType { Standard, MultiVariate }
