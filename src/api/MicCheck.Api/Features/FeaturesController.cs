using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Features;

[ApiController]
[Route("api/v1/projects/{projectId}/features")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class FeaturesController(FeatureService featureService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<FeatureResponse>>> List(
        int projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var all = await featureService.ListByProjectAsync(projectId, ct);
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).Select(FeatureResponse.From).ToList();
        return Ok(new PaginatedResponse<FeatureResponse>(all.Count, null, null, paged));
    }

    [HttpPost]
    public async Task<ActionResult<FeatureResponse>> Create(
        int projectId, CreateFeatureRequest request, CancellationToken ct)
    {
        try
        {
            var feature = await featureService.CreateAsync(projectId, request.Name, request.Type, request.InitialValue, request.Description, ct);

            return CreatedAtAction(nameof(GetById), new { projectId, id = feature.Id }, FeatureResponse.From(feature));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FeatureResponse>> GetById(int projectId, int id, CancellationToken ct)
    {
        var feature = await featureService.FindByIdAsync(id, ct);
        if (feature is null || feature.ProjectId != projectId) return NotFound();
        return Ok(FeatureResponse.From(feature));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FeatureResponse>> Update(
        int projectId, int id, UpdateFeatureRequest request, CancellationToken ct)
    {
        var feature = await featureService.FindByIdAsync(id, ct);
        if (feature is null || feature.ProjectId != projectId) return NotFound();

        var updated = await featureService.UpdateAsync(id, request.Name, request.Description, ct);
        return Ok(FeatureResponse.From(updated));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<FeatureResponse>> Patch(
        int projectId, int id, PatchFeatureRequest request, CancellationToken ct)
    {
        var feature = await featureService.FindByIdAsync(id, ct);
        if (feature is null || feature.ProjectId != projectId) return NotFound();

        if (request.Name is not null) feature.Name = request.Name;
        if (request.Description is not null) feature.Description = request.Description;
        if (request.DefaultEnabled.HasValue) feature.DefaultEnabled = request.DefaultEnabled.Value;

        var updated = await featureService.UpdateAsync(id, feature.Name, feature.Description, ct);
        return Ok(FeatureResponse.From(updated));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int projectId, int id, CancellationToken ct)
    {
        var feature = await featureService.FindByIdAsync(id, ct);
        if (feature is null || feature.ProjectId != projectId) return NotFound();

        await featureService.DeleteAsync(id, ct);
        return NoContent();
    }
}
