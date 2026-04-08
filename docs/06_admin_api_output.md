# Step 6 Output: Admin API

## Summary

Implemented the Admin API — authenticated management endpoints for organizations, projects, environments, features, segments, identities, tags, webhooks, and audit logs. Authenticated via `Authorization: Api-Key <TOKEN>` or JWT bearer token.

---

## What Was Built

### Shared Utilities

**`MicCheck.Api/Common/DomainException.cs`**
- Custom exception for business rule violations (feature limit, segment limit, etc.)
- Caught in controllers and returned as `400 BadRequest`

**`MicCheck.Api/Common/PaginatedResponse.cs`**
- Generic `PaginatedResponse<T>(Count, Next, Previous, Results)` record used by all list endpoints

---

### Audit

**`MicCheck.Api/Audit/AuditService.cs`**
- `virtual LogAsync(resourceType, resourceId, action, organizationId, projectId?, environmentId?, changes?, ct)` — persists audit log entry, extracts actor user ID from `UserId` claim via `IHttpContextAccessor`

**`MicCheck.Api/Audit/AuditLogQueryService.cs`**
- `ListByOrganizationAsync`, `ListByProjectAsync`, `ListByEnvironmentAsync` — ordered by descending `CreatedAt`

**`MicCheck.Api/Audit/AuditLogResponse.cs`**
- Static `From(AuditLog)` factory

**`MicCheck.Api/Audit/AuditLogsController.cs`**
- `GET /api/v1/organisations/{id}/audit-logs`
- `GET /api/v1/projects/{projectId}/audit-logs`
- (Environment audit logs handled in `EnvironmentsController`)

---

### Organizations

**`MicCheck.Api/Organizations/OrganizationService.cs`**
- `ListForUserAsync(userId)` — returns orgs where user is a member
- `CreateAsync(name, creatorUserId)` — auto-adds creator as Admin member
- `UpdateAsync`, `DeleteAsync`, `FindByIdAsync`
- `ListMembersAsync`, `InviteUserAsync` (upsert), `RemoveMemberAsync`

**`MicCheck.Api/Organizations/OrganizationsController.cs`**
Routes:
- `GET /api/v1/organisations/` — paginated, filtered to calling user's orgs
- `POST /api/v1/organisations/`
- `GET /api/v1/organisations/{id}/`
- `PUT /api/v1/organisations/{id}/`
- `DELETE /api/v1/organisations/{id}/`
- `GET /api/v1/organisations/{id}/users/`
- `POST /api/v1/organisations/{id}/users/invite/`
- `DELETE /api/v1/organisations/{id}/users/{userId}/`

---

### Projects

**`MicCheck.Api/Projects/ProjectService.cs`**
- `ListByOrganizationAsync`, `FindByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- `ListUserPermissionsAsync`, `SetUserPermissionsAsync` (upsert), `RemoveUserPermissionsAsync`

**`MicCheck.Api/Projects/ProjectsController.cs`**
Routes:
- `GET /api/v1/projects/?organizationId=`
- `POST /api/v1/projects/`
- `GET /api/v1/projects/{id}/`
- `PUT /api/v1/projects/{id}/`
- `DELETE /api/v1/projects/{id}/`
- `GET /api/v1/projects/{id}/user-permissions/`
- `POST /api/v1/projects/{id}/user-permissions/`
- `PUT /api/v1/projects/{id}/user-permissions/{userId}/`
- `DELETE /api/v1/projects/{id}/user-permissions/{userId}/`

---

### Environments

**`MicCheck.Api/Environments/EnvironmentService.cs`**
- `CreateAsync` — generates a random API key via `ApiKeyHasher.GenerateKey()`, auto-creates `FeatureState` for all existing features
- `CloneAsync` — duplicates environment-level `FeatureState` records only (not identity or segment overrides)
- `UpdateAsync`, `DeleteAsync`, `FindByApiKeyAsync`, `ListByProjectAsync`

**`MicCheck.Api/Environments/EnvironmentsController.cs`**
Routes:
- `GET /api/v1/environments/?projectId=`
- `POST /api/v1/environments/`
- `GET /api/v1/environments/{apiKey}/`
- `PUT /api/v1/environments/{apiKey}/`
- `DELETE /api/v1/environments/{apiKey}/`
- `POST /api/v1/environments/{apiKey}/clone/`
- `GET /api/v1/environments/{apiKey}/webhooks/`
- `POST /api/v1/environments/{apiKey}/webhooks/`
- `PUT /api/v1/environments/{apiKey}/webhooks/{id}/`
- `DELETE /api/v1/environments/{apiKey}/webhooks/{id}/`
- `GET /api/v1/environments/{apiKey}/audit-logs/`
- `GET /api/v1/environments/{apiKey}/identities/` (paginated)
- `GET /api/v1/environments/{apiKey}/identities/{id}/`
- `DELETE /api/v1/environments/{apiKey}/identities/{id}/`
- `GET /api/v1/environments/{apiKey}/identities/{id}/featurestates/`
- `PUT /api/v1/environments/{apiKey}/identities/{id}/featurestates/{featureId}/`
- `DELETE /api/v1/environments/{apiKey}/identities/{id}/featurestates/{featureId}/`
- `GET /api/v1/environments/{apiKey}/featurestates/`
- `GET /api/v1/environments/{apiKey}/featurestates/{id}/`
- `PUT /api/v1/environments/{apiKey}/featurestates/{id}/`
- `PATCH /api/v1/environments/{apiKey}/featurestates/{id}/`

---

### Features

**`MicCheck.Api/Features/FeatureService.cs`**
- `CreateAsync` — enforces 400-feature limit (`DomainException`), auto-creates `FeatureState` for all project environments
- `UpdateAsync`, `DeleteAsync`, `FindByIdAsync`, `ListByProjectAsync`

**`MicCheck.Api/Features/FeatureStateService.cs`**
- `ListByEnvironmentAsync` — environment-level states only (no identity/segment overrides)
- `FindByIdAsync`, `UpdateAsync` (full replace), `PatchAsync` (partial)

**`MicCheck.Api/Features/FeaturesController.cs`**
Routes under `api/v1/projects/{projectId}/features/`:
- `GET`, `POST`, `GET {id}`, `PUT {id}`, `PATCH {id}`, `DELETE {id}`

---

### Segments

**`MicCheck.Api/Segments/SegmentService.cs`**
- `CreateAsync` — enforces 100-segment limit, 100-condition limit (across all rules recursively)
- `UpdateAsync` — replaces all rules and conditions (full replace semantics)
- `DeleteAsync`, `FindByIdAsync`, `ListByProjectAsync`

**`MicCheck.Api/Segments/SegmentsController.cs`**
Routes under `api/v1/projects/{projectId}/segments/`:
- `GET`, `POST`, `GET {id}`, `PUT {id}`, `DELETE {id}`

---

### Admin Identities

**`MicCheck.Api/Identities/AdminIdentityService.cs`**
- `ListAsync` (paginated), `FindByIdAsync`, `DeleteAsync`
- `GetFeatureStatesAsync`, `SetFeatureStateAsync` (upsert), `DeleteFeatureStateAsync`

---

### Tags

**`MicCheck.Api/Features/TagService.cs`** — list, create, delete

**`MicCheck.Api/Features/TagsController.cs`**
Routes under `api/v1/projects/{projectId}/tags/`:
- `GET`, `POST`, `DELETE {id}`

---

### Webhooks

**`MicCheck.Api/Webhooks/WebhookService.cs`** — list by environment, create, update, delete

---

## Request Validation

All incoming DTOs have FluentValidation validators in the same folder. Validation errors return **422 Unprocessable Entity** with field-level detail (configured via `ApiBehaviorOptions`).

Key validation rules:
- Feature names: `^[a-zA-Z0-9_-]+$`, max 150 chars
- Feature `InitialValue`: max 20,000 chars
- Webhook URLs: must be absolute URI
- Tag colors: hex format (`#RGB` or `#RRGGBB`)
- Segment rule `Type`: `All`, `Any`, or `None`
- Segment condition `Operator`: any of the 17 `SegmentConditionOperator` enum values

---

## Program.cs Changes

- Added `JsonStringEnumConverter` to MVC JSON options (allows `"Standard"` instead of `0` in API requests)
- Added `Configure<ApiBehaviorOptions>` to return 422 with field-level errors for validation failures
- Registered new services: `AuditService`, `AuditLogQueryService`, `OrganizationService`, `ProjectService`, `EnvironmentService`, `FeatureService`, `FeatureStateService`, `SegmentService`, `TagService`, `WebhookService`, `AdminIdentityService`

---

## Deviations from Plan

- Identity admin endpoints are nested under `/api/v1/environments/{apiKey}/` (as specified in the plan) and handled in `EnvironmentsController` via `[FromServices]` to avoid constructor over-injection
- Feature state endpoints (`/api/v1/environments/{apiKey}/featurestates/`) are also in `EnvironmentsController` for the same reason
- `AuditService.LogAsync` is marked `virtual` to support Moq in service unit tests

---

## Tests Added

### `FeatureServiceTests` (6 tests, in-memory DB)
- Creating a feature auto-creates `FeatureState` for each environment
- `InitialValue` is propagated to `FeatureState.Value`
- 400-feature limit throws `DomainException`
- Update changes name and description
- Delete removes the feature
- List returns all project features

### `EnvironmentServiceTests` (5 tests, in-memory DB)
- Creating an environment auto-creates `FeatureState` for each existing feature
- Each created environment gets a unique API key
- Clone copies environment-level feature states
- Clone does NOT copy identity overrides
- Clone generates a different API key from source

### `SegmentServiceTests` (6 tests, in-memory DB)
- Create persists segment, rules, and conditions
- 100-segment limit throws `DomainException`
- 100-condition limit throws `DomainException`
- Update replaces old rules with new rules
- Delete removes segment
- List returns all project segments

### `AdminApiIntegrationTests` (7 tests, WebApplicationFactory)
- Creating a project returns 201
- Creating a feature returns 201
- Creating a feature auto-creates `FeatureState` for environments
- Creating an environment auto-creates `FeatureState` for existing features
- Missing API key returns 401
- Invalid feature name (with spaces) returns 422
- Creating a segment returns 201

---

## Test Results

```
Passed! - Failed: 0, Passed: 166, Skipped: 0, Total: 166
```
