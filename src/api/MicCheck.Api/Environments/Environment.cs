using MicCheck.Api.Features;

namespace MicCheck.Api.Environments;

public class Environment
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public required string ApiKey { get; set; }
    public int ProjectId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<FeatureState> FeatureStates { get; init; } = [];
}
