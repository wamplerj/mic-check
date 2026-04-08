using MicCheck.Api.Authorization;
using MicCheck.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Organizations;

[ApiController]
[Route("api/v1/organisations")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class OrganizationsController(OrganizationService organizationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<OrganizationResponse>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var all = await organizationService.ListForUserAsync(userId.Value, ct);
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).Select(OrganizationResponse.From).ToList();

        return Ok(new PaginatedResponse<OrganizationResponse>(all.Count, null, null, paged));
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationResponse>> Create(
        CreateOrganizationRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var org = await organizationService.CreateAsync(request.Name, userId.Value, ct);
        return CreatedAtAction(nameof(GetById), new { id = org.Id }, OrganizationResponse.From(org));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationResponse>> GetById(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();
        return Ok(OrganizationResponse.From(org));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OrganizationResponse>> Update(
        int id, UpdateOrganizationRequest request, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var updated = await organizationService.UpdateAsync(id, request.Name, ct);
        return Ok(OrganizationResponse.From(updated));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        await organizationService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id}/users")]
    public async Task<ActionResult<IReadOnlyList<OrganizationMemberResponse>>> ListUsers(
        int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var members = await organizationService.ListMembersAsync(id, ct);
        return Ok(members.Select(OrganizationMemberResponse.From).ToList());
    }

    [HttpPost("{id}/users/invite")]
    public async Task<IActionResult> InviteUser(int id, InviteUserRequest request, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var role = Enum.Parse<OrganizationRole>(request.Role, ignoreCase: true);
        await organizationService.InviteUserAsync(id, request.UserId, role, ct);
        return Ok();
    }

    [HttpDelete("{id}/users/{userId}")]
    public async Task<IActionResult> RemoveUser(int id, int userId, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        await organizationService.RemoveMemberAsync(id, userId, ct);
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("UserId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
