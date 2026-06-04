using MicCheck.Api.Common.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicCheck.Api.Features;

[ApiController]
[Route("api/v1/flags")]
[Authorize(Policy = AuthorizationPolicies.FlagsApiAccess)]
public class FlagsController(FeatureEvaluationService featureEvaluationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlagResponse>>> GetAll(CancellationToken ct)
    {
        var environmentId = int.Parse(User.FindFirst("EnvironmentId")!.Value);
        var results = await featureEvaluationService.EvaluateForEnvironmentAsync(environmentId, ct);
        return Ok(results.Select(FlagResponse.From).ToList());
    }
}
