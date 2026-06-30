using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common;
using MicCheck.Api.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Organizations;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class OrganizationsController(OrganizationService organizationService, WebhookService webhookService) : ControllerBase
{
    [HttpGet("api/v1/organisations")]
    public async Task<ActionResult<PaginatedResponse<OrganizationResponse>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var all = await organizationService.ListForUserAsync(userId.Value, ct);
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => OrganizationResponse.From(x.Org, x.IsPrimary)).ToList();

        return Ok(new PaginatedResponse<OrganizationResponse>(all.Count, null, null, paged));
    }

    [HttpPost("api/v1/organisations")]
    public async Task<ActionResult<OrganizationResponse>> Create(
        CreateOrganizationRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var org = await organizationService.CreateAsync(request.Name, userId.Value, ct);
        return CreatedAtAction(nameof(GetById), new { id = org.Id }, OrganizationResponse.From(org));
    }

    [HttpGet("api/v1/organisation/{id}")]
    public async Task<ActionResult<OrganizationResponse>> GetById(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();
        return Ok(OrganizationResponse.From(org));
    }

    [HttpPut("api/v1/organisation/{id}")]
    public async Task<ActionResult<OrganizationResponse>> Update(
        int id, UpdateOrganizationRequest request, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var updated = await organizationService.UpdateAsync(id, request.Name, ct);
        return Ok(OrganizationResponse.From(updated));
    }

    [HttpPut("api/v1/organisation/{id}/primary")]
    public async Task<IActionResult> SetPrimary(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await organizationService.SetPrimaryAsync(id, userId.Value, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("api/v1/organisation/{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        await organizationService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("api/v1/organisation/{id}/users")]
    public async Task<ActionResult<IReadOnlyList<OrganizationMemberResponse>>> ListUsers(
        int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var members = await organizationService.ListMembersAsync(id, ct);
        return Ok(members.Select(OrganizationMemberResponse.From).ToList());
    }

    [HttpPost("api/v1/organisation/{id}/users/invite")]
    public async Task<IActionResult> InviteUser(int id, InviteUserRequest request, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var role = Enum.Parse<OrganizationRole>(request.Role, ignoreCase: true);
        await organizationService.InviteUserAsync(id, request.UserId, role, ct);
        return Ok();
    }

    [HttpPost("api/v1/organisation/{id}/users/invite-by-email")]
    public async Task<ActionResult<IReadOnlyList<InviteByEmailResult>>> InviteUsersByEmail(
        int id, InviteUsersByEmailRequest request, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var results = await organizationService.InviteUsersByEmailAsync(id, request.Invites, ct);
        return Ok(results);
    }

    [HttpGet("api/v1/organisation/{id}/invite-link")]
    public async Task<ActionResult<InviteTokenResponse>> GetInviteLink(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var token = await organizationService.GetOrCreateInviteTokenAsync(id, ct);
        return Ok(new InviteTokenResponse(token));
    }

    [HttpPost("api/v1/organisation/{id}/invite-link/regenerate")]
    public async Task<ActionResult<InviteTokenResponse>> RegenerateInviteLink(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var token = await organizationService.RegenerateInviteTokenAsync(id, ct);
        return Ok(new InviteTokenResponse(token));
    }

    [HttpPost("api/v1/organisations/invite/{token}/accept")]
    public async Task<IActionResult> AcceptInvite(string token, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await organizationService.AcceptInviteAsync(token, userId.Value, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Invalid or expired invite link.");
        }
    }

    [HttpDelete("api/v1/organisation/{id}/user/{userId}")]
    public async Task<IActionResult> RemoveUser(int id, int userId, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        await organizationService.RemoveMemberAsync(id, userId, ct);
        return NoContent();
    }

    [HttpGet("api/v1/organisation/{id}/webhooks")]
    public async Task<ActionResult<IReadOnlyList<WebhookResponse>>> ListWebhooks(int id, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var webhooks = await webhookService.ListByOrganizationAsync(id, ct);
        return Ok(webhooks.Select(WebhookResponse.From).ToList());
    }

    [HttpPost("api/v1/organisation/{id}/webhooks")]
    public async Task<ActionResult<WebhookResponse>> CreateWebhook(
        int id, CreateWebhookRequest request, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var webhook = await webhookService.CreateForOrganizationAsync(id, request.Url, request.Secret, request.Enabled, ct);
        return CreatedAtAction(nameof(ListWebhooks), new { id }, WebhookResponse.From(webhook));
    }

    [HttpPut("api/v1/organisation/{id}/webhook/{webhookId}")]
    public async Task<ActionResult<WebhookResponse>> UpdateWebhook(
        int id, int webhookId, CreateWebhookRequest request, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var webhook = await webhookService.FindByIdAsync(webhookId, ct);
        if (webhook is null || webhook.OrganizationId != id) return NotFound();

        var updated = await webhookService.UpdateAsync(webhookId, request.Url, request.Secret, request.Enabled, ct);
        return Ok(WebhookResponse.From(updated));
    }

    [HttpDelete("api/v1/organisation/{id}/webhook/{webhookId}")]
    public async Task<IActionResult> DeleteWebhook(int id, int webhookId, CancellationToken ct)
    {
        var org = await organizationService.FindByIdAsync(id, ct);
        if (org is null) return NotFound();

        var webhook = await webhookService.FindByIdAsync(webhookId, ct);
        if (webhook is null || webhook.OrganizationId != id) return NotFound();

        await webhookService.DeleteAsync(webhookId, ct);
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
