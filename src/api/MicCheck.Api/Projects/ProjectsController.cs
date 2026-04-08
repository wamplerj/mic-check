using MicCheck.Api.Authorization;
using MicCheck.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Projects;

[ApiController]
[Route("api/v1/projects")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class ProjectsController(ProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProjectResponse>>> List(
        [FromQuery] int organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var all = await projectService.ListByOrganizationAsync(organizationId, ct);
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).Select(ProjectResponse.From).ToList();
        return Ok(new PaginatedResponse<ProjectResponse>(all.Count, null, null, paged));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var project = await projectService.CreateAsync(request.OrganizationId, request.Name, ct);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, ProjectResponse.From(project));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectResponse>> GetById(int id, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(id, ct);
        if (project is null) return NotFound();
        return Ok(ProjectResponse.From(project));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectResponse>> Update(int id, UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(id, ct);
        if (project is null) return NotFound();

        var updated = await projectService.UpdateAsync(id, request.Name, request.HideDisabledFlags, ct);
        return Ok(ProjectResponse.From(updated));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(id, ct);
        if (project is null) return NotFound();

        await projectService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id}/user-permissions")]
    public async Task<ActionResult<IReadOnlyList<UserPermissionResponse>>> ListUserPermissions(
        int id, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(id, ct);
        if (project is null) return NotFound();

        var perms = await projectService.ListUserPermissionsAsync(id, ct);
        return Ok(perms.Select(UserPermissionResponse.From).ToList());
    }

    [HttpPost("{id}/user-permissions")]
    public async Task<ActionResult<UserPermissionResponse>> CreateUserPermissions(
        int id, SetUserPermissionsRequest request, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(id, ct);
        if (project is null) return NotFound();

        var permissions = request.Permissions
            .Select(p => Enum.Parse<Authorization.ProjectPermission>(p, ignoreCase: true))
            .ToList();

        var perm = await projectService.SetUserPermissionsAsync(id, request.UserId, request.IsAdmin, permissions, ct);
        return Ok(UserPermissionResponse.From(perm));
    }

    [HttpPut("{id}/user-permissions/{userId}")]
    public async Task<ActionResult<UserPermissionResponse>> UpdateUserPermissions(
        int id, int userId, SetUserPermissionsRequest request, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(id, ct);
        if (project is null) return NotFound();

        var permissions = request.Permissions
            .Select(p => Enum.Parse<Authorization.ProjectPermission>(p, ignoreCase: true))
            .ToList();

        var perm = await projectService.SetUserPermissionsAsync(id, userId, request.IsAdmin, permissions, ct);
        return Ok(UserPermissionResponse.From(perm));
    }

    [HttpDelete("{id}/user-permissions/{userId}")]
    public async Task<IActionResult> DeleteUserPermissions(int id, int userId, CancellationToken ct)
    {
        var project = await projectService.FindByIdAsync(id, ct);
        if (project is null) return NotFound();

        await projectService.RemoveUserPermissionsAsync(id, userId, ct);
        return NoContent();
    }
}
