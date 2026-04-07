namespace MicCheck.Api.Identities;

public record IdentityRequest(string Identifier, IReadOnlyList<TraitInput>? Traits);
