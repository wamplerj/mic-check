using MicCheck.Api.Common.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Features;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
[Route("api/v1/project/{projectId}")]
public class TagsController(TagService tagService) : ControllerBase
{
    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<TagResponse>>> List(int projectId, CancellationToken ct)
    {
        var tags = await tagService.ListByProjectAsync(projectId, ct);
        return Ok(tags.Select(TagResponse.From).ToList());
    }

    [HttpPost("tags")]
    public async Task<ActionResult<TagResponse>> Create(int projectId, CreateTagRequest request, CancellationToken ct)
    {
        var tag = await tagService.CreateAsync(projectId, request.Label, request.Color, ct);
        return CreatedAtAction(nameof(List), new { projectId }, TagResponse.From(tag));
    }

    [HttpDelete("tag/{id}")]
    public async Task<IActionResult> Delete(int projectId, int id, CancellationToken ct)
    {
        var tag = await tagService.FindByIdAsync(id, ct);
        if (tag is null || tag.ProjectId != projectId) return NotFound();

        await tagService.DeleteAsync(id, ct);
        return NoContent();
    }
}
