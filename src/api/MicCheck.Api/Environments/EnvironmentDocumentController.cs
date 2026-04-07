using MicCheck.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicCheck.Api.Environments;

[ApiController]
[Route("api/v1/environment-document")]
[Authorize(Policy = AuthorizationPolicies.FlagsApiAccess)]
public class EnvironmentDocumentController(EnvironmentDocumentService environmentDocumentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EnvironmentDocumentResponse>> Get(CancellationToken ct)
    {
        var environmentId = int.Parse(User.FindFirst("EnvironmentId")!.Value);
        var document = await environmentDocumentService.GetAsync(environmentId, ct);

        return document is null ? NotFound() : Ok(document);
    }
}
