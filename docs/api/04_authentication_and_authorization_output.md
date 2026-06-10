# Step 4 Output: Authentication & Authorization

## Summary

Implemented all three authentication schemes, authorization policies, role-based access control, and the full auth/API key endpoint suite.

---

## What Was Built

### Authentication Schemes

**`EnvironmentKeyAuthenticationHandler`** (`MicCheck.Api/Authentication/`)
- Reads `X-Environment-Key` header
- Looks up `Environment` by `ApiKey` in the database
- Sets `EnvironmentId` and `ProjectId` claims on success
- Scheme name constant: `EnvironmentKeyAuthenticationHandler.SchemeName`

**`ApiKeyAuthenticationHandler`** (`MicCheck.Api/Authentication/`)
- Parses `Authorization: Api-Key <TOKEN>` header
- Hashes the raw token with SHA-256 and looks it up in `ApiKeys` table
- Validates `IsActive` and `ExpiresAt`
- Sets `OrganizationId` and `OrganizationRole = Admin` claims on success (API keys grant org-admin access)

**JWT Bearer** — existing `TokenService` updated to accept `User` (not a username string), now embeds `sub` (userId), `email`, `given_name`, `family_name`, plus `OrganizationId` and `OrganizationRole` claims for each org membership.

### Authorization Policies (`MicCheck.Api/Authorization/`)

| Policy | Schemes | Requirement |
|---|---|---|
| `FlagsApiAccess` | EnvironmentKey | `EnvironmentId` claim present |
| `AdminApiAccess` | ApiKey, Bearer | Authenticated user |
| `OrganizationAdmin` | ApiKey, Bearer | `OrganizationRole = Admin` claim |

### RBAC (`MicCheck.Api/Authorization/`)

- `ProjectPermission` enum (12 values: ViewProject, CreateFeature, EditFeature, etc.)
- `UserProjectPermission` entity — `(UserId, ProjectId)` composite key, `IsAdmin` flag, `List<ProjectPermission>` stored as comma-separated string via EF Core value converter
- `ProjectPermissionRequirement` — `IAuthorizationRequirement` wrapping a `ProjectPermission`
- `ProjectPermissionRequirementHandler` — resolves project ID from route values (`projectId`), bypasses check for org admins, checks `UserProjectPermission` record otherwise

### Auth Service & Endpoints (`MicCheck.Api/Auth/`)

**`AuthService`** — login, register, refresh, logout:
- Login: verifies password with `IPasswordHasher<User>`, issues JWT + 30-day refresh token
- Register: creates `User`, default `Organization`, joins as `Admin`, issues tokens
- Refresh: revokes old refresh token, issues new JWT + new refresh token (rotation)
- Logout: revokes refresh token

**Endpoints** at `/api/v1/auth/` (all anonymous):

| Method | Path | Description |
|---|---|---|
| POST | `/api/v1/auth/login` | Returns `LoginResponse(AccessToken, RefreshToken, ExpiresAt)` |
| POST | `/api/v1/auth/register` | Creates user + org, returns same |
| POST | `/api/v1/auth/refresh` | Rotates refresh token |
| POST | `/api/v1/auth/logout` | Revokes refresh token, returns 204 |

### API Key Management (`MicCheck.Api/ApiKeys/`)

**`ApiKeyHasher`** — static utility:
- `GenerateKey()` — `RandomNumberGenerator.GetBytes(32)` → Base64URL (no `+`, `/`, `=`)
- `Hash(key)` — SHA-256 hex string (64 chars, lowercase)

**`ApiKeyService`** — create, list, revoke

**Endpoints** at `/api/v1/organisations/{organizationId}/api-keys/` (require `OrganizationAdmin`):

| Method | Path | Description |
|---|---|---|
| POST | `/` | Returns `CreateApiKeyResponse` including the raw key (shown once only) |
| GET | `/` | Returns list with prefix only (hashed key never returned) |
| DELETE | `/{keyId}` | Sets `IsActive = false` |

### New Domain Types

- `RefreshToken` (`MicCheck.Api/Users/`) — with EF Core cascade delete on user
- `UserProjectPermission` (`MicCheck.Api/Authorization/`)
- New request/response records: `LoginRequest`, `LoginResponse`, `RegisterRequest`, `RefreshRequest`, `LogoutRequest`, `CreateApiKeyRequest`, `CreateApiKeyResponse`, `ApiKeyResponse`

### New EF Core Configurations

- `RefreshTokenConfiguration` — unique index on `Token`, index on `UserId`, cascade delete
- `UserProjectPermissionConfiguration` — composite key `(UserId, ProjectId)`, `Permissions` stored as comma-separated string

---

## Deviations from Plan

- `UserProjectPermission.Permissions` declared as `List<ProjectPermission>` (not `ICollection`) to ensure EF Core value converter works correctly
- API keys granted `OrganizationRole = Admin` claim — necessary for `OrganizationAdmin` policy (which `RequireClaim("OrganizationRole", "Admin")`); without it, API-key-authenticated requests could never manage API keys

---

## Tests Added

### `ApiKeyHasherTests` (7 tests)
- Hash is deterministic
- Hash is lowercase hex (64 chars)
- Different inputs produce different hashes
- Generated key is non-empty and unique
- Generated key is Base64URL-safe (no `+`, `/`, `=`)
- Generated key can be verified by hashing

### `AuthEndpointsTests` (10 tests)
End-to-end via `WebApplicationFactory` with in-memory DB:
- Register with valid details → 200 with tokens
- Register duplicate email → 409
- Login with correct credentials → 200 with tokens
- Login with wrong password → 401
- Login with unknown email → 401
- Login with empty email → 400
- Refresh with valid token → 200 with new tokens
- Refresh with invalid token → 401
- Refresh with already-used (revoked) token → 401
- Logout → 204, subsequent refresh → 401

### `ApiKeyAuthenticationHandlerTests` (5 tests)
Via `WebApplicationFactory` against the API key management endpoints:
- Missing auth header → 401
- Bearer scheme (not Api-Key) → 401
- Invalid key → 401
- Inactive key → 401
- Expired key → 401
- Valid key → 200

### `ProjectPermissionRequirementHandlerTests` (7 tests)
Direct unit tests with in-memory DB:
- Org admin bypasses all project checks → succeeds
- User with required permission → succeeds
- Project admin (`IsAdmin = true`) satisfies any permission → succeeds
- User without required permission → fails
- User with no permission record → fails
- Missing user ID claim → fails
- Missing project ID in route → fails

### `TokenServiceTests` (5 tests, updated)
Updated for new `User`-based signature — verifies token non-empty, email claim, expiry, issuer, and org claims.

---

## Issues Encountered

1. **`WebApplicationFactory` + multiple EF Core providers** — Removing `DbContextOptions<MicCheckDbContext>` alone does not remove Npgsql's `IDatabaseProvider`. Fixed by using `UseInternalServiceProvider` with a dedicated `InMemoryEfServiceProvider` (static), which tells EF Core to bypass the application DI container for provider discovery entirely.

2. **Per-request Guid DB name** — Initially `Guid.NewGuid()` was evaluated inside the `AddDbContext` options lambda, generating a new DB name per `DbContext` creation. Each HTTP request therefore got an empty database. Fixed by capturing the Guid once in `ConfigureWebHost` before the `ConfigureServices` lambda.

---

## Test Results

```
Passed! - Failed: 0, Passed: 102, Skipped: 0, Total: 102
```
