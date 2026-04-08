using MicCheck.Api.Authorization;
using MicCheck.Api.Projects;
using MicCheck.Api.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Audit;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class AuditLogsController(
    AuditLogQueryService auditLogQueryService,
    OrganizationService organizationService,
    ProjectService projectService) : ControllerBase
{
    [HttpGet("api/v1/organisations/{id}/audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> ListByOrganization(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var logs = await auditLogQueryService.ListByOrganizationAsync(id, ct);
        return Ok(logs.Select(AuditLogResponse.From).ToList());
    }

    [HttpGet("api/v1/projects/{projectId}/audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> ListByProject(int projectId, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(projectId, ct);
        if (project is null) return NotFound();

        var logs = await auditLogQueryService.ListByProjectAsync(projectId, ct);
        return Ok(logs.Select(AuditLogResponse.From).ToList());
    }
}
