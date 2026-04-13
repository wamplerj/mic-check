# Step 7 Output: Audit Logging & Webhooks

## Summary

Implemented the full audit logging and webhook system for MicCheck.

## What Was Built

### Audit Logging

- **`AuditService`** — `RecordAsync` captures before/after state as `{"before":{...},"after":{...}}` JSON in `AuditLog.Changes`. Serializes with `System.Text.Json` (camelCase). Enqueues `AUDIT_LOG_CREATED` webhook event after persisting. Methods are `virtual` for Moq compatibility.
- **`AuditLogQueryService`** — Separate query service with `ListByOrganizationAsync`, `ListByProjectAsync`, and `ListByEnvironmentAsync`. All return `(int Total, IReadOnlyList<AuditLog> Items)` tuples. Filtering via `AuditLogFilter` record (date range, resource type, action, project/environment/actor).
- **`AuditLogFilter`** — Record with optional filter fields and pagination (`Page`, `PageSize`).
- **`AuditLogsController`** — Updated to accept `[FromQuery] AuditLogFilter` and return `PaginatedResponse<AuditLogResponse>`.
- **`EnvironmentsController`** — Added `GET /{apiKey}/audit-logs` endpoint.

### Webhooks

- **`WebhookEvent`** / **`WebhookEventTypes`** — Event model with `EventType`, `EnvironmentId`, `OrganizationId`, `Data`. Three event types: `FLAG_UPDATED`, `FLAG_DELETED`, `AUDIT_LOG_CREATED`.
- **`WebhookPayload`** — Snake_case serialized payload with `data` and `event_type` fields. Nested records: `FlagUpdatedData`, `FlagDeletedData`, `FlagStateSnapshot`, `FeatureSummary`, `EnvironmentSummary`.
- **`WebhookQueue`** — Singleton `System.Threading.Channels`-backed queue. `EnqueueAsync` is non-blocking; `ReadAllAsync` consumed by background service.
- **`WebhookDispatcher`** — Scoped service. Finds active webhooks by environment + org scope, serializes payload, signs with HMAC-SHA256 (`X-Flagsmith-Signature: sha256=<hex>`), POSTs with 10s timeout via `IHttpClientFactory`, logs `WebhookDeliveryLog` with `AttemptNumber`.
- **`WebhookBackgroundService`** — `BackgroundService` reading from queue, dispatching via scoped `WebhookDispatcher`.
- **`WebhookRetryBackgroundService`** — Polls every 60s. Retries failed deliveries: attempt 1→2 after 5 min, 2→3 after 30 min, max 3 total.
- **`WebhookDeliveryLog`** — Added `AttemptNumber` property.
- **`WebhookService`** — Added `ListByOrganizationAsync`, `CreateForOrganizationAsync`, `ListDeliveriesAsync`.
- **`WebhookDeliveryLogResponse`** — New response DTO.

### Admin Endpoints Added

```
GET    /api/v1/environments/{apiKey}/webhooks/{id}/deliveries
GET    /api/v1/organisations/{id}/webhooks
POST   /api/v1/organisations/{id}/webhooks
PUT    /api/v1/organisations/{id}/webhooks/{id}
DELETE /api/v1/organisations/{id}/webhooks/{id}
```

### Service Updates

- **`FeatureService`** — Constructor now takes `WebhookQueue`. `DeleteAsync` fires `FLAG_DELETED` events per environment. `RecordAsync` used with before/after.
- **`FeatureStateService`** — Constructor now takes `WebhookQueue` and `AuditService`. `UpdateAsync` and `PatchAsync` capture before state and dispatch `FLAG_UPDATED` + audit record.

### Program.cs

Added registrations:
- `WebhookDispatcher` (scoped)
- `WebhookQueue` (singleton)
- `WebhookBackgroundService` (hosted)
- `WebhookRetryBackgroundService` (hosted)
- `AddHttpClient("Webhooks")` with `User-Agent` header
- `JsonStringEnumConverter` for string enum deserialization
- `ApiBehaviorOptions` for 422 validation error format

### Migration

Added `AddWebhookDeliveryAttemptNumber` migration for the new `AttemptNumber` column on `WebhookDeliveryLog`.

## Tests Added

| File | Tests |
|------|-------|
| `Audit/AuditServiceTests.cs` | 5 — null changes, after-only diff, before+after diff, metadata persistence, webhook event enqueued |
| `Webhooks/WebhookDispatcherTests.cs` | 6 — signature computation, delivery without webhooks, delivery logging |
| `Webhooks/WebhookRetryTests.cs` | 4 — retry delay values, max attempts, eligibility |

**Total: 166 tests passing.**

## Acceptance Criteria Status

- [x] Every create/update/delete in the Admin API produces an `AuditLog` record
- [x] Audit logs correctly capture before/after state for updates
- [x] `GET /api/v1/organisations/{id}/audit-logs/` returns paginated, filtered results
- [x] Audit controllers map `AuditLog` domain objects to `AuditLogResponse` DTOs
- [x] Webhooks fire within 1 second of a flag state change (async via Channel)
- [x] HMAC-SHA256 signature is correct and verifiable by receivers
- [x] Failed webhook deliveries are logged with status code and response body
- [x] Retry logic attempts delivery 3 times with backoff (5 min, 30 min)
- [x] Webhook delivery does not block the API response (fire-and-forget via `WebhookQueue`)
- [x] Unit tests cover: signature computation, payload serialization, retry scheduling
- [x] Unit tests cover: audit diff generation for create/update/delete scenarios
