using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Segments;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class SegmentsController(SegmentService segmentService) : ControllerBase
{
    [HttpGet("api/v1/project/{projectId}/segments")]
    public async Task<ActionResult<PaginatedResponse<SegmentResponse>>> List(
        int projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var all = await segmentService.ListByProjectAsync(projectId, ct);
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).Select(SegmentResponse.From).ToList();
        return Ok(new PaginatedResponse<SegmentResponse>(all.Count, null, null, paged));
    }

    [HttpPost("api/v1/project/{projectId}/segments")]
    public async Task<ActionResult<SegmentResponse>> Create(
        int projectId, CreateSegmentRequest request, CancellationToken ct)
    {
        try
        {
            var rules = MapRules(request.Rules);
            var segment = await segmentService.CreateAsync(projectId, request.Name, rules, ct);
            return CreatedAtAction(nameof(GetById), new { projectId, id = segment.Id }, SegmentResponse.From(segment));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("api/v1/project/{projectId}/segment/{id}")]
    public async Task<ActionResult<SegmentResponse>> GetById(int projectId, int id, CancellationToken ct)
    {
        var segment = await segmentService.FindByIdAsync(id, ct);
        if (segment is null || segment.ProjectId != projectId) return NotFound();
        return Ok(SegmentResponse.From(segment));
    }

    [HttpPut("api/v1/project/{projectId}/segment/{id}")]
    public async Task<ActionResult<SegmentResponse>> Update(
        int projectId, int id, CreateSegmentRequest request, CancellationToken ct)
    {
        var segment = await segmentService.FindByIdAsync(id, ct);
        if (segment is null || segment.ProjectId != projectId) return NotFound();

        try
        {
            var rules = MapRules(request.Rules);
            var updated = await segmentService.UpdateAsync(id, request.Name, rules, ct);
            return Ok(SegmentResponse.From(updated));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("api/v1/project/{projectId}/segment/{id}")]
    public async Task<IActionResult> Delete(int projectId, int id, CancellationToken ct)
    {
        var segment = await segmentService.FindByIdAsync(id, ct);
        if (segment is null || segment.ProjectId != projectId) return NotFound();

        await segmentService.DeleteAsync(id, ct);
        return NoContent();
    }

    private static IReadOnlyList<SegmentRuleDefinition> MapRules(IReadOnlyList<CreateSegmentRuleRequest> rules) =>
        rules.Select(r => new SegmentRuleDefinition(
            Enum.Parse<SegmentRuleType>(r.Type, ignoreCase: true),
            r.Conditions.Select(c => new SegmentConditionDefinition(
                c.Property,
                Enum.Parse<SegmentConditionOperator>(c.Operator, ignoreCase: true),
                c.Value)).ToList(),
            r.ChildRules is not null ? MapRules(r.ChildRules) : null
        )).ToList();
}
