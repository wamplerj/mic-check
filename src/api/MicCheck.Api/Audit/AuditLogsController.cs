using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common;
using MicCheck.Api.Environments;
using MicCheck.Api.Projects;
using MicCheck.Api.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Audit;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
[Route("api/v1")]
public class AuditLogsController(
    AuditLogQueryService auditLogQueryService,
    OrganizationService organizationService,
    ProjectService projectService,
    EnvironmentService environmentService) : ControllerBase
{
    [HttpGet("organisation/{id}/audit-logs")]
    public async Task<ActionResult<PaginatedResponse<AuditLogResponse>>> ListByOrganization(int id, [FromQuery] AuditLogFilter filter, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var result = await auditLogQueryService.ListByOrganizationAsync(id, filter, ct);
        return Ok(new PaginatedResponse<AuditLogResponse>(result.Total, null, null, result.Items.ToList()));
    }

    [HttpGet("project/{projectId}/audit-logs")]
    public async Task<ActionResult<PaginatedResponse<AuditLogResponse>>> ListByProject(int projectId, [FromQuery] AuditLogFilter filter, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(projectId, ct);
        if (project is null) return NotFound();

        var result = await auditLogQueryService.ListByProjectAsync(projectId, filter, ct);
        return Ok(new PaginatedResponse<AuditLogResponse>(result.Total, null, null, result.Items.ToList()));
    }

    [HttpGet("environment/{apiKey}/audit-logs")]
    public async Task<ActionResult<PaginatedResponse<AuditLogResponse>>> ListByEnvironment(string apiKey, [FromQuery] AuditLogFilter filter, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var result = await auditLogQueryService.ListByEnvironmentAsync(environment.Id, filter, ct);
        return Ok(new PaginatedResponse<AuditLogResponse>(result.Total, null, null, result.Items.ToList()));
    }
}
