# Step 7: Audit Logging & Webhooks

## Goal
Implement the audit trail system (recording all changes to flags, environments, segments, etc.) and the webhook system (notifying external systems of flag state changes).

---

## Audit Logging

### What Gets Audited

Every create, update, and delete operation on:
- Features and FeatureStates
- Environments
- Segments
- Identities (admin operations)
- API Keys (create/revoke)
- User permissions
- Organization settings
- Project settings

### AuditLog Domain Model

```csharp
// MicCheck.Api/Audit/AuditLog.cs (already defined in Step 2)
// Key fields:
//   ResourceType: "Feature", "FeatureState", "Environment", etc.
//   ResourceId: string representation of the entity's PK
//   Action: "Created", "Updated", "Deleted"
//   Changes: JSON diff of before/after values (for Updates)
//   ActorUserId: null for API key operations without user context
```

### Audit Service

Lives in `MicCheck.Api/Audit/`. Injects `MicCheckDbContext` directly.

```csharp
// MicCheck.Api/Audit/AuditService.cs
public class AuditService
{
    private readonly MicCheckDbContext _db;

    public Task RecordAsync(
        string resourceType,
        string resourceId,
        string action,
        int organizationId,
        int? projectId = null,
        int? environmentId = null,
        int? actorUserId = null,
        object? before = null,
        object? after = null,
        CancellationToken ct = default);
}
```

**Change diffing:**
- Serialize `before` and `after` objects to JSON
- Store both in `Changes` as `{ "before": {...}, "after": {...} }`
- Use `System.Text.Json` for serialization

### Integration with Services

Audit logging is called directly from other service methods:

```csharp
// In FeatureService.CreateAsync:
await _auditService.RecordAsync(
    resourceType: "Feature",
    resourceId: feature.Id.ToString(),
    action: "Created",
    organizationId: project.OrganizationId,
    projectId: feature.ProjectId,
    after: feature);
```

### Audit Log Queries

`AuditService` exposes a query method returning domain objects; the controller maps to response DTOs.

Support filtering by:
- Date range (`from`, `to`)
- Resource type
- Action type
- Project
- Environment
- Actor user

Always ordered by `CreatedAt DESC`. Paginated (default 20, max 100).

```csharp
// MicCheck.Api/Audit/AuditController.cs
[HttpGet]
public async Task<ActionResult<PaginatedResponse<AuditLogResponse>>> List(
    int id, [FromQuery] AuditLogFilter filter, CancellationToken ct)
{
    var (logs, total) = await _auditService.ListAsync(id, filter, ct);
    return Ok(new PaginatedResponse<AuditLogResponse>(
        total, null, null,
        logs.Select(AuditLogResponse.From).ToList()));
}
```

---

## Webhooks

### Webhook Triggers

Webhooks fire on:
- `FLAG_UPDATED` — any `FeatureState` change
- `FLAG_DELETED` — feature deleted
- `AUDIT_LOG_CREATED` — any audit log entry (organization webhooks)

### Webhook Payload

```json
{
  "data": {
    "changed_by": "user@example.com",
    "timestamp": "2026-04-06T12:00:00Z",
    "new_state": {
      "id": 42,
      "enabled": true,
      "feature_state_value": null,
      "feature": { "id": 1, "name": "dark_mode" },
      "environment": { "id": 5, "name": "Production" }
    },
    "previous_state": { ... }
  },
  "event_type": "FLAG_UPDATED"
}
```

### Webhook Event & Dispatcher

Live in `MicCheck.Api/Webhooks/`. Dispatcher injects `MicCheckDbContext` directly.

```csharp
// MicCheck.Api/Webhooks/WebhookEvent.cs
public class WebhookEvent
{
    public required string EventType { get; init; }
    public required object Data { get; init; }
    public int? EnvironmentId { get; init; }
    public int OrganizationId { get; init; }
}

// MicCheck.Api/Webhooks/WebhookDispatcher.cs
public class WebhookDispatcher
{
    private readonly MicCheckDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    // Finds all active webhooks for the environment (and org-level)
    // Serializes payload to JSON
    // Signs payload with HMAC-SHA256 using webhook secret
    // POSTs to webhook URL with timeout of 10 seconds
    // Stores delivery result in WebhookDeliveryLog
    public Task DispatchAsync(WebhookEvent webhookEvent, CancellationToken ct = default);
}
```

### Webhook Signing

```csharp
// HMAC-SHA256 signature in header:
// X-Flagsmith-Signature: sha256=<hex_digest>

private static string ComputeSignature(string secret, string payload)
{
    var keyBytes = Encoding.UTF8.GetBytes(secret);
    var payloadBytes = Encoding.UTF8.GetBytes(payload);
    var hashBytes = HMACSHA256.HashData(keyBytes, payloadBytes);
    return "sha256=" + Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```

### Webhook Queue (Async Dispatch)

Lives in `MicCheck.Api/Webhooks/`. Uses `System.Threading.Channels` so dispatch does not block the API response.

```csharp
// MicCheck.Api/Webhooks/WebhookQueue.cs
public class WebhookQueue
{
    private readonly Channel<WebhookEvent> _channel = Channel.CreateUnbounded<WebhookEvent>();

    public ValueTask EnqueueAsync(WebhookEvent webhookEvent) =>
        _channel.Writer.WriteAsync(webhookEvent);

    public IAsyncEnumerable<WebhookEvent> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
```

### Retry Background Service

Lives in `MicCheck.Infrastructure/Webhooks/`. Polls `WebhookDeliveryLog` and retries failures.

```csharp
// MicCheck.Infrastructure/Webhooks/WebhookRetryBackgroundService.cs
public class WebhookRetryBackgroundService : BackgroundService
{
    // Polls WebhookDeliveryLog for failed deliveries within retry window
    // Up to 3 attempts: immediate, +5 min, +30 min
    // Reattempts via WebhookDispatcher
}
```

### Retry Strategy

- Up to 3 delivery attempts (immediate, +5 min, +30 min)
- Failed deliveries are logged but do not block the API response

---

## Webhook Admin Endpoints

Controllers map domain objects to/from DTOs in the action method, calling `WebhookService` for all data access.

```
GET    /api/v1/environments/{apiKey}/webhooks/
POST   /api/v1/environments/{apiKey}/webhooks/
PUT    /api/v1/environments/{apiKey}/webhooks/{id}/
DELETE /api/v1/environments/{apiKey}/webhooks/{id}/
GET    /api/v1/environments/{apiKey}/webhooks/{id}/deliveries/

GET    /api/v1/organisations/{id}/webhooks/
POST   /api/v1/organisations/{id}/webhooks/
PUT    /api/v1/organisations/{id}/webhooks/{id}/
DELETE /api/v1/organisations/{id}/webhooks/{id}/
```

---

## Acceptance Criteria
- [ ] Every create/update/delete in the Admin API produces an `AuditLog` record
- [ ] Audit logs correctly capture before/after state for updates
- [ ] `GET /api/v1/organisations/{id}/audit-logs/` returns paginated, filtered results
- [ ] Audit controllers map `AuditLog` domain objects to `AuditLogResponse` DTOs in the action
- [ ] Webhooks fire within 1 second of a flag state change
- [ ] HMAC-SHA256 signature is correct and verifiable by receivers
- [ ] Failed webhook deliveries are logged with status code and response body
- [ ] Retry logic attempts delivery 3 times with backoff
- [ ] Webhook delivery does not block the API response (fire-and-forget via `WebhookQueue`)
- [ ] Unit tests cover: signature computation, payload serialization, retry scheduling
- [ ] Unit tests cover: audit diff generation for create/update/delete scenarios
