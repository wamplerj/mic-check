namespace MicCheck.Api.Identities;

public record TraitResponse(string Key, string Value)
{
    public static TraitResponse From(IdentityTrait trait) =>
        new(trait.Key, trait.Value);
}
