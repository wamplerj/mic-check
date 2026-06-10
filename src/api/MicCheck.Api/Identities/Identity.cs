using MicCheck.Api.Features;

namespace MicCheck.Api.Identities;

public class Identity
{
    public int Id { get; init; }
    public required string Identifier { get; set; }
    public int EnvironmentId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<IdentityTrait> Traits { get; init; } = [];
    public ICollection<FeatureState> FeatureStateOverrides { get; init; } = [];
}
