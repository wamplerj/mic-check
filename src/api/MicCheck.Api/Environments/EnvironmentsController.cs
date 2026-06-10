using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common;
using MicCheck.Api.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MicCheck.Api.Environments;

[ApiController]
[Route("api/v1/environments")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class EnvironmentsController(EnvironmentService environmentService, WebhookService webhookService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<EnvironmentResponse>>> List(
        [FromQuery] int projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var all = await environmentService.ListByProjectAsync(projectId, ct);
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).Select(EnvironmentResponse.From).ToList();
        return Ok(new PaginatedResponse<EnvironmentResponse>(all.Count, null, null, paged));
    }

    [HttpPost]
    public async Task<ActionResult<EnvironmentResponse>> Create(CreateEnvironmentRequest request, CancellationToken ct)
    {
        var environment = await environmentService.CreateAsync(request.ProjectId, request.Name, ct);
        return CreatedAtAction(nameof(GetByApiKey), new { apiKey = environment.ApiKey }, EnvironmentResponse.From(environment));
    }

    [HttpGet("{apiKey}")]
    public async Task<ActionResult<EnvironmentResponse>> GetByApiKey(string apiKey, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();
        return Ok(EnvironmentResponse.From(environment));
    }

    [HttpPut("{apiKey}")]
    public async Task<ActionResult<EnvironmentResponse>> Update(string apiKey, UpdateEnvironmentRequest request, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var updated = await environmentService.UpdateAsync(apiKey, request.Name, ct);
        return Ok(EnvironmentResponse.From(updated));
    }

    [HttpDelete("{apiKey}")]
    public async Task<IActionResult> Delete(string apiKey, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        await environmentService.DeleteAsync(apiKey, ct);
        return NoContent();
    }

    [HttpPost("{apiKey}/clone")]
    public async Task<ActionResult<EnvironmentResponse>> Clone(string apiKey, CloneEnvironmentRequest request, CancellationToken ct)
    {
        var source = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (source is null) return NotFound();

        var cloned = await environmentService.CloneAsync(apiKey, request.Name, ct);
        return CreatedAtAction(nameof(GetByApiKey), new { apiKey = cloned.ApiKey }, EnvironmentResponse.From(cloned));
    }

    [HttpGet("{apiKey}/webhooks")]
    public async Task<ActionResult<IReadOnlyList<WebhookResponse>>> ListWebhooks(string apiKey, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var webhooks = await webhookService.ListByEnvironmentAsync(environment.Id, ct);
        return Ok(webhooks.Select(WebhookResponse.From).ToList());
    }

    [HttpPost("{apiKey}/webhooks")]
    public async Task<ActionResult<WebhookResponse>> CreateWebhook(
        string apiKey, CreateWebhookRequest request, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var webhook = await webhookService.CreateAsync(environment.Id, request.Url, request.Secret, request.Enabled, ct);
        return CreatedAtAction(nameof(GetByApiKey), new { apiKey }, WebhookResponse.From(webhook));
    }

    [HttpPut("{apiKey}/webhooks/{id}")]
    public async Task<ActionResult<WebhookResponse>> UpdateWebhook(
        string apiKey, int id, CreateWebhookRequest request, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var webhook = await webhookService.FindByIdAsync(id, ct);
        if (webhook is null || webhook.EnvironmentId != environment.Id) return NotFound();

        var updated = await webhookService.UpdateAsync(id, request.Url, request.Secret, request.Enabled, ct);
        return Ok(WebhookResponse.From(updated));
    }

    [HttpGet("{apiKey}/webhooks/{id}/deliveries")]
    public async Task<ActionResult<IReadOnlyList<WebhookDeliveryLogResponse>>> ListWebhookDeliveries(
        string apiKey, int id, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var webhook = await webhookService.FindByIdAsync(id, ct);
        if (webhook is null || webhook.EnvironmentId != environment.Id) return NotFound();

        var logs = await webhookService.ListDeliveriesAsync(id, ct);
        return Ok(logs.Select(WebhookDeliveryLogResponse.From).ToList());
    }

    [HttpDelete("{apiKey}/webhooks/{id}")]
    public async Task<IActionResult> DeleteWebhook(string apiKey, int id, CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var webhook = await webhookService.FindByIdAsync(id, ct);
        if (webhook is null || webhook.EnvironmentId != environment.Id) return NotFound();

        await webhookService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{apiKey}/audit-logs")]
    public async Task<ActionResult<IReadOnlyList<Audit.AuditLogResponse>>> ListAuditLogs(
        string apiKey,
        [FromServices] Audit.AuditLogQueryService auditLogQueryService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var (_, logs) = await auditLogQueryService.ListByEnvironmentAsync(environment.Id, new Audit.AuditLogFilter(), ct);
        return Ok(logs.ToList());
    }

    [HttpGet("{apiKey}/identities")]
    public async Task<ActionResult<PaginatedResponse<Identities.AdminIdentityResponse>>> ListIdentities(
        string apiKey,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var (total, items) = await adminIdentityService.ListAsync(environment.Id, page, pageSize, ct);
        var results = items.Select(Identities.AdminIdentityResponse.From).ToList();
        return Ok(new PaginatedResponse<Identities.AdminIdentityResponse>(total, null, null, results));
    }

    [HttpPost("{apiKey}/identities")]
    public async Task<ActionResult<Identities.AdminIdentityResponse>> CreateIdentity(
        string apiKey,
        [FromBody] Identities.CreateIdentityRequest request,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var identity = await adminIdentityService.CreateAsync(environment.Id, request.Identifier, ct);
        if (identity is null)
            return Conflict(new { error = "An identity with this identifier already exists in the environment." });

        return CreatedAtAction(nameof(GetIdentity), new { apiKey, id = identity.Id },
            Identities.AdminIdentityResponse.From(identity));
    }

    [HttpGet("{apiKey}/identities/{id}")]
    public async Task<ActionResult<Identities.AdminIdentityResponse>> GetIdentity(
        string apiKey, int id,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var identity = await adminIdentityService.FindByIdAsync(id, environment.Id, ct);
        if (identity is null) return NotFound();
        return Ok(Identities.AdminIdentityResponse.From(identity));
    }

    [HttpDelete("{apiKey}/identities/{id}")]
    public async Task<IActionResult> DeleteIdentity(
        string apiKey, int id,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var identity = await adminIdentityService.FindByIdAsync(id, environment.Id, ct);
        if (identity is null) return NotFound();

        await adminIdentityService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{apiKey}/identities/{id}/traits/{key}")]
    public async Task<ActionResult<Identities.TraitResponse>> UpsertIdentityTrait(
        string apiKey, int id, string key,
        [FromBody] Identities.UpsertTraitRequest request,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var result = await adminIdentityService.UpsertTraitAsync(id, environment.Id, key, request.Value, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{apiKey}/identities/{id}/traits/{key}")]
    public async Task<IActionResult> DeleteIdentityTrait(
        string apiKey, int id, string key,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var deleted = await adminIdentityService.DeleteTraitAsync(id, environment.Id, key, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{apiKey}/identities/{id}/featurestates")]
    public async Task<ActionResult<IReadOnlyList<Features.FeatureStateResponse>>> GetIdentityFeatureStates(
        string apiKey, int id,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var identity = await adminIdentityService.FindByIdAsync(id, environment.Id, ct);
        if (identity is null) return NotFound();

        var states = await adminIdentityService.GetFeatureStatesAsync(id, environment.Id, ct);
        return Ok(states.Select(Features.FeatureStateResponse.From).ToList());
    }

    [HttpPut("{apiKey}/identities/{id}/featurestates/{featureId}")]
    public async Task<ActionResult<Features.FeatureStateResponse>> SetIdentityFeatureState(
        string apiKey, int id, int featureId,
        Features.UpdateFeatureStateRequest request,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var identity = await adminIdentityService.FindByIdAsync(id, environment.Id, ct);
        if (identity is null) return NotFound();

        var state = await adminIdentityService.SetFeatureStateAsync(id, environment.Id, featureId, request.Enabled, request.Value, ct);
        return Ok(Features.FeatureStateResponse.From(state));
    }

    [HttpDelete("{apiKey}/identities/{id}/featurestates/{featureId}")]
    public async Task<IActionResult> DeleteIdentityFeatureState(
        string apiKey, int id, int featureId,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var identity = await adminIdentityService.FindByIdAsync(id, environment.Id, ct);
        if (identity is null) return NotFound();

        await adminIdentityService.DeleteFeatureStateAsync(id, environment.Id, featureId, ct);
        return NoContent();
    }

    [HttpGet("{apiKey}/featurestates")]
    public async Task<ActionResult<IReadOnlyList<Features.FeatureStateResponse>>> ListFeatureStates(
        string apiKey,
        [FromServices] Features.FeatureStateService featureStateService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var states = await featureStateService.ListByEnvironmentAsync(environment.Id, ct);
        return Ok(states.Select(Features.FeatureStateResponse.From).ToList());
    }

    [HttpGet("{apiKey}/featurestates/{id}")]
    public async Task<ActionResult<Features.FeatureStateResponse>> GetFeatureState(
        string apiKey, int id,
        [FromServices] Features.FeatureStateService featureStateService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var state = await featureStateService.FindByIdAsync(id, environment.Id, ct);
        if (state is null) return NotFound();
        return Ok(Features.FeatureStateResponse.From(state));
    }

    [HttpPut("{apiKey}/featurestates/{id}")]
    public async Task<ActionResult<Features.FeatureStateResponse>> UpdateFeatureState(
        string apiKey, int id,
        Features.UpdateFeatureStateRequest request,
        [FromServices] Features.FeatureStateService featureStateService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var state = await featureStateService.FindByIdAsync(id, environment.Id, ct);
        if (state is null) return NotFound();

        var updated = await featureStateService.UpdateAsync(id, request.Enabled, request.Value, ct);
        return Ok(Features.FeatureStateResponse.From(updated));
    }

    [HttpPatch("{apiKey}/featurestates/{id}")]
    public async Task<ActionResult<Features.FeatureStateResponse>> PatchFeatureState(
        string apiKey, int id,
        Features.PatchFeatureStateRequest request,
        [FromServices] Features.FeatureStateService featureStateService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var state = await featureStateService.FindByIdAsync(id, environment.Id, ct);
        if (state is null) return NotFound();

        var updated = await featureStateService.PatchAsync(id, request.Enabled, request.Value, ct);
        return Ok(Features.FeatureStateResponse.From(updated));
    }

    // ─── Feature Segments ────────────────────────────────────────────────────

    [HttpGet("{apiKey}/features/{featureId}/segments")]
    public async Task<ActionResult<IReadOnlyList<Features.FeatureSegmentResponse>>> ListFeatureSegments(
        string apiKey, int featureId,
        [FromServices] Features.FeatureSegmentService featureSegmentService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var results = await featureSegmentService.ListByFeatureAsync(featureId, environment.Id, ct);
        return Ok(results);
    }

    [HttpPost("{apiKey}/features/{featureId}/segments")]
    public async Task<ActionResult<Features.FeatureSegmentResponse>> CreateFeatureSegment(
        string apiKey, int featureId,
        Features.CreateFeatureSegmentRequest request,
        [FromServices] Features.FeatureSegmentService featureSegmentService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        try
        {
            var result = await featureSegmentService.CreateAsync(
                featureId, environment.Id, request.SegmentId, request.Priority,
                request.Enabled, request.Value, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("{apiKey}/features/{featureId}/segments/{id}")]
    public async Task<ActionResult<Features.FeatureSegmentResponse>> UpdateFeatureSegment(
        string apiKey, int featureId, int id,
        Features.UpdateFeatureSegmentRequest request,
        [FromServices] Features.FeatureSegmentService featureSegmentService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var result = await featureSegmentService.UpdateAsync(id, request.Priority, request.Enabled, request.Value, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{apiKey}/features/{featureId}/segments/{id}")]
    public async Task<IActionResult> DeleteFeatureSegment(
        string apiKey, int featureId, int id,
        [FromServices] Features.FeatureSegmentService featureSegmentService,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var deleted = await featureSegmentService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ─── Identity Segments ───────────────────────────────────────────────────

    [HttpGet("{apiKey}/identities/{id}/segments")]
    public async Task<ActionResult<IReadOnlyList<Segments.SegmentSummaryResponse>>> GetIdentitySegments(
        string apiKey, int id,
        [FromServices] Identities.AdminIdentityService adminIdentityService,
        [FromServices] Segments.SegmentService segmentService,
        [FromServices] Segments.SegmentEvaluator segmentEvaluator,
        CancellationToken ct)
    {
        var environment = await environmentService.FindByApiKeyAsync(apiKey, ct);
        if (environment is null) return NotFound();

        var identity = await adminIdentityService.FindByIdAsync(id, environment.Id, ct);
        if (identity is null) return NotFound();

        var segments = await segmentService.ListByProjectAsync(environment.ProjectId, ct);
        var matching = segments
            .Where(s => segmentEvaluator.Evaluate(s, identity.Traits.ToList(), identity.Identifier))
            .Select(s => new Segments.SegmentSummaryResponse(s.Id, s.Name))
            .ToList();

        return Ok(matching);
    }
}
