namespace MicCheck.Api.Identities;

public record AdminIdentityResponse(
    int Id,
    string Identifier,
    int EnvironmentId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TraitResponse> Traits
)
{
    public static AdminIdentityResponse From(Identity identity) => new(
        identity.Id,
        identity.Identifier,
        identity.EnvironmentId,
        identity.CreatedAt,
        identity.Traits.Select(t => new TraitResponse(t.Key, t.Value)).ToList());
}
