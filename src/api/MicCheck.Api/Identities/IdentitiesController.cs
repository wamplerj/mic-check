using MicCheck.Api.Authorization;
using MicCheck.Api.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicCheck.Api.Identities;

[ApiController]
[Route("api/v1/identities")]
[Authorize(Policy = AuthorizationPolicies.FlagsApiAccess)]
public class IdentitiesController(
    FeatureEvaluationService featureEvaluationService,
    IdentityResolutionService identityResolutionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IdentityResponse>> GetByIdentifier(
        [FromQuery] string identifier, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return BadRequest("identifier query parameter is required.");

        var environmentId = int.Parse(User.FindFirst("EnvironmentId")!.Value!);

        var identity = await identityResolutionService.ResolveAsync(environmentId, identifier, null, ct);
        var flags = await featureEvaluationService.EvaluateForIdentityAsync(environmentId, identifier, null, ct);

        return Ok(new IdentityResponse(
            identity.Traits.Select(TraitResponse.From).ToList(),
            flags.Select(FlagResponse.From).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<IdentityResponse>> Identify(
        [FromBody] IdentityRequest request, CancellationToken ct)
    {
        var environmentId = int.Parse(User.FindFirst("EnvironmentId")!.Value!);

        var identity = await identityResolutionService.ResolveAsync(
            environmentId, request.Identifier, request.Traits, ct);
        var flags = await featureEvaluationService.EvaluateForIdentityAsync(
            environmentId, request.Identifier, request.Traits, ct);

        return Ok(new IdentityResponse(
            identity.Traits.Select(TraitResponse.From).ToList(),
            flags.Select(FlagResponse.From).ToList()));
    }
}
