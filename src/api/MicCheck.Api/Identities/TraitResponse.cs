namespace MicCheck.Api.Identities;

public record TraitResponse(string TraitKey, string TraitValue)
{
    public static TraitResponse From(IdentityTrait trait) =>
        new(trait.Key, trait.Value);
}
