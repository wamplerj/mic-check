using MicCheck.Api.Common.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Features;

[ApiController]
[Route("api/v1/projects/{projectId}/tags")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class TagsController(TagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TagResponse>>> List(int projectId, CancellationToken ct)
    {
        var tags = await tagService.ListByProjectAsync(projectId, ct);
        return Ok(tags.Select(TagResponse.From).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<TagResponse>> Create(int projectId, CreateTagRequest request, CancellationToken ct)
    {
        var tag = await tagService.CreateAsync(projectId, request.Label, request.Color, ct);
        return CreatedAtAction(nameof(List), new { projectId }, TagResponse.From(tag));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int projectId, int id, CancellationToken ct)
    {
        var tag = await tagService.FindByIdAsync(id, ct);
        if (tag is null || tag.ProjectId != projectId) return NotFound();

        await tagService.DeleteAsync(id, ct);
        return NoContent();
    }
}
