using MicCheck.Api.Features;

namespace MicCheck.Api.Identities;

public record IdentityResponse(
    IReadOnlyList<TraitResponse> Traits,
    IReadOnlyList<FlagResponse> Flags);
