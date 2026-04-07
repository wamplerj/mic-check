# Step 3 Output: Database Schema & EF Core

## Summary

All database infrastructure from the plan was implemented. The `DbContext` was placed in `MicCheck.Api/Data/` (not `MicCheck.Infrastructure`) to avoid a circular dependency, since `MicCheck.Api` already references `MicCheck.Infrastructure`.

---

## What Was Built

### `MicCheck.Api/Data/MicCheckDbContext.cs`
- 18 `DbSet<T>` properties covering all domain entities
- `OnModelCreating` delegates to `ApplyConfigurationsFromAssembly`
- Uses `using AppEnvironment = MicCheck.Api.Environments.Environment;` alias to avoid collision with `System.Environment`

### `MicCheck.Api/Data/Configurations/` (18 files)
One `IEntityTypeConfiguration<T>` per entity with all constraints:

| Entity | Key constraints |
|---|---|
| `Feature` | Name max 150, InitialValue max 20k, unique `(ProjectId, Name)` |
| `FeatureState` | Value max 20k, `Version` as concurrency token, unique `(FeatureId, EnvironmentId, IdentityId)`, indexes on `EnvironmentId` and `IdentityId` |
| `IdentityTrait` | Value max 2k, unique `(IdentityId, Key)` |
| `Environment` | ApiKey max 100, unique index on `ApiKey` |
| `ApiKey` | Key max 512, Prefix max 8, unique index on `Key` |
| `Identity` | unique `(EnvironmentId, Identifier)` |
| `AuditLog` | index on `(OrganizationId, CreatedAt)` |
| `Webhook` | index on `EnvironmentId` |

### `MicCheck.Api/Data/DatabaseUrlParser.cs`
Parses `DATABASE_URL` (postgresql://user:pass@host:port/db) into an Npgsql connection string. Defaults port to 5432 when not specified.

### `MicCheck.Api/Data/DatabaseSeeder.cs`
Seeds a default Organization ("Default"), Project ("My Project"), and three Environments (Development, Staging, Production) on first startup when no organizations exist. Only runs in the Development environment.

### `Program.cs` updates
- Registers `MicCheckDbContext` with Npgsql using either `DATABASE_URL` or `ConnectionStrings:DefaultConnection`
- Runs the seeder only when `app.Environment.IsDevelopment()`

---

## Deviations from Plan

- **DbContext location**: Plan specified `MicCheck.Infrastructure/Data/`. Moved to `MicCheck.Api/Data/` due to circular dependency constraint. `MicCheck.Infrastructure` is reserved for background services (Step 7).
- **Migrations**: Not added in this step — requires a running PostgreSQL instance. Run manually with `dotnet ef migrations add InitialSchema --project src/api/MicCheck.Api`.

---

## Tests Added

### `MicCheck.Api.Tests.Unit/Data/MicCheckDbContextTests.cs` (10 tests)
Covers all major DbSet operations using `UseInMemoryDatabase`:
- Organization CRUD
- Project query by organization
- Environment query by API key
- Feature query by project
- FeatureState query by environment
- Segment with included rules
- Identity with included traits
- AuditLog filter by organization
- Webhook filter by environment
- ApiKey retrieval by hashed key
- User retrieval by email

### `MicCheck.Api.Tests.Unit/Data/DatabaseUrlParserTests.cs` (4 tests)
- Full URL with all components
- Default port (5432) when omitted
- Custom port preserved
- Special characters in password preserved

### `MicCheck.Api.Tests.Unit/Auth/AuthEndpointsTests.cs` (3 tests, fixed)
Integration tests using `WebApplicationFactory<Program>`. Fixed by:
- Setting `UseEnvironment("Testing")` so the seeder doesn't run at startup
- Overriding `ITokenService` with a Moq mock

---

## Issues Encountered

1. **`System.Environment` name collision** — `MicCheck.Api.Environments.Environment` conflicts with `System.Environment` pulled in by implicit usings. Resolved everywhere with the `using AppEnvironment = ...` alias.

2. **`WebApplicationFactory` + seeder crash** — The seeder runs in Development mode and tries to create a `MicCheckDbContext`, which requires a real Npgsql connection. Test factory fixed by setting `UseEnvironment("Testing")` to prevent the seeder block from executing.

---

## Test Results

```
Passed! - Failed: 0, Passed: 73, Skipped: 0, Total: 73
```
