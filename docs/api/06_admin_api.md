# Step 6: Admin API (Management Endpoints)

## Goal
Implement the Admin API — the authenticated management API for creating and managing projects, environments, features, segments, users, and API keys. Authenticated via `Authorization: Api-Key <TOKEN>` or JWT bearer token.

---

## API Versioning

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
```

Routes:
- Current: `/api/v1/`
- Future: `/api/v2/`

All Admin API controllers:
```csharp
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
```

---

## Endpoint Groups

### Organizations

```
GET    /api/v1/organisations/
POST   /api/v1/organisations/
GET    /api/v1/organisations/{id}/
PUT    /api/v1/organisations/{id}/
DELETE /api/v1/organisations/{id}/
GET    /api/v1/organisations/{id}/users/
POST   /api/v1/organisations/{id}/users/invite/
DELETE /api/v1/organisations/{id}/users/{userId}/
```

### Projects

```
GET    /api/v1/projects/
POST   /api/v1/projects/
GET    /api/v1/projects/{id}/
PUT    /api/v1/projects/{id}/
DELETE /api/v1/projects/{id}/
GET    /api/v1/projects/{id}/user-permissions/
POST   /api/v1/projects/{id}/user-permissions/
PUT    /api/v1/projects/{id}/user-permissions/{userId}/
DELETE /api/v1/projects/{id}/user-permissions/{userId}/
```

### Environments

```
GET    /api/v1/environments/
POST   /api/v1/environments/
GET    /api/v1/environments/{apiKey}/
PUT    /api/v1/environments/{apiKey}/
DELETE /api/v1/environments/{apiKey}/
POST   /api/v1/environments/{apiKey}/clone/
GET    /api/v1/environments/{apiKey}/webhooks/
POST   /api/v1/environments/{apiKey}/webhooks/
PUT    /api/v1/environments/{apiKey}/webhooks/{id}/
DELETE /api/v1/environments/{apiKey}/webhooks/{id}/
```

### Features

```
GET    /api/v1/projects/{projectId}/features/
POST   /api/v1/projects/{projectId}/features/
GET    /api/v1/projects/{projectId}/features/{id}/
PUT    /api/v1/projects/{projectId}/features/{id}/
PATCH  /api/v1/projects/{projectId}/features/{id}/
DELETE /api/v1/projects/{projectId}/features/{id}/
```

### Feature States (Environment-level)

```
GET    /api/v1/environments/{apiKey}/featurestates/
GET    /api/v1/environments/{apiKey}/featurestates/{id}/
PUT    /api/v1/environments/{apiKey}/featurestates/{id}/
PATCH  /api/v1/environments/{apiKey}/featurestates/{id}/
```

### Segments

```
GET    /api/v1/projects/{projectId}/segments/
POST   /api/v1/projects/{projectId}/segments/
GET    /api/v1/projects/{projectId}/segments/{id}/
PUT    /api/v1/projects/{projectId}/segments/{id}/
DELETE /api/v1/projects/{projectId}/segments/{id}/
```

### Identities (Admin view)

```
GET    /api/v1/environments/{apiKey}/identities/
GET    /api/v1/environments/{apiKey}/identities/{id}/
DELETE /api/v1/environments/{apiKey}/identities/{id}/
GET    /api/v1/environments/{apiKey}/identities/{id}/featurestates/
PUT    /api/v1/environments/{apiKey}/identities/{id}/featurestates/{featureId}/
DELETE /api/v1/environments/{apiKey}/identities/{id}/featurestates/{featureId}/
```

### Audit Logs

```
GET    /api/v1/organisations/{id}/audit-logs/
GET    /api/v1/projects/{projectId}/audit-logs/
GET    /api/v1/environments/{apiKey}/audit-logs/
```

### Tags

```
GET    /api/v1/projects/{projectId}/tags/
POST   /api/v1/projects/{projectId}/tags/
DELETE /api/v1/projects/{projectId}/tags/{id}/
```

---

## Service Layer

Each feature area has a service class in its folder within `MicCheck.Api`. Services accept and return domain objects — they have no knowledge of request/response DTOs. Services inject `MicCheckDbContext` directly.

```csharp
// MicCheck.Api/Features/FeatureService.cs
public class FeatureService
{
    private readonly MicCheckDbContext _db;
    private readonly AuditService _auditService;

    // Enforces: max 400 features per project
    // On create: auto-creates FeatureState records for all existing environments
    public Task<Feature> CreateAsync(int projectId, string name, FeatureType type,
        string? initialValue, string? description, CancellationToken ct = default);

    public Task<Feature> UpdateAsync(int id, string name, string? description,
        CancellationToken ct = default);

    public Task DeleteAsync(int id, CancellationToken ct = default);
    
    public Task<Feature?> FindByIdAsync(int id, CancellationToken ct = default);
    
    public Task<IReadOnlyList<Feature>> ListByProjectAsync(int projectId,
        CancellationToken ct = default);
}

// MicCheck.Api/Environments/EnvironmentService.cs
public class EnvironmentService
{
    private readonly MicCheckDbContext _db;
    private readonly AuditService _auditService;

    // On create: auto-creates FeatureState records for all existing features
    public Task<Environment> CreateAsync(int projectId, string name, CancellationToken ct = default);

    // Clone: duplicates all environment-level FeatureStates; not identity/segment overrides
    public Task<Environment> CloneAsync(string sourceApiKey, string newName, CancellationToken ct = default);
}

// MicCheck.Api/Segments/SegmentService.cs
public class SegmentService
{
    private readonly MicCheckDbContext _db;
    private readonly AuditService _auditService;

    // Enforces: max 100 segments per project
    // Enforces: max 100 segment rule conditions
    public Task<Segment> CreateAsync(int projectId, string name,
        IReadOnlyList<SegmentRuleDefinition> rules, CancellationToken ct = default);
}
```

---

## Controller Pattern

Controllers are responsible for:
1. Receiving the request DTO
2. Validating it (FluentValidation runs automatically via middleware)
3. Calling the appropriate service with extracted domain-level values
4. Mapping the returned domain object(s) to a response DTO
5. Returning the HTTP response

```csharp
// MicCheck.Api/Features/FeaturesController.cs
[ApiController]
[Route("api/v1/projects/{projectId}/features")]
[Authorize(Policy = AuthorizationPolicies.AdminApiAccess)]
[EnableRateLimiting("AdminApi")]
public class FeaturesController : ControllerBase
{
    private readonly FeatureService _featureService;

    [HttpPost]
    public async Task<ActionResult<FeatureResponse>> Create(
        int projectId, CreateFeatureRequest request, CancellationToken ct)
    {
        var feature = await _featureService.CreateAsync(
            projectId, request.Name, request.Type, request.InitialValue, request.Description, ct);

        return CreatedAtAction(nameof(GetById), new { projectId, id = feature.Id },
            FeatureResponse.From(feature));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FeatureResponse>> GetById(int projectId, int id, CancellationToken ct)
    {
        var feature = await _featureService.FindByIdAsync(id, ct);
        if (feature is null) return NotFound();
        return Ok(FeatureResponse.From(feature));
    }
}
```

---

## Request Validation

Use FluentValidation for all incoming request DTOs. Validators live in the same folder as the DTO they validate.

```csharp
// MicCheck.Api/Features/CreateFeatureRequest.cs
public record CreateFeatureRequest(
    string Name,
    FeatureType Type,
    string? InitialValue,
    string? Description
);

// MicCheck.Api/Features/CreateFeatureRequestValidator.cs
public class CreateFeatureRequestValidator : AbstractValidator<CreateFeatureRequest>
{
    public CreateFeatureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .Matches("^[a-zA-Z0-9_-]+$");

        RuleFor(x => x.InitialValue)
            .MaximumLength(20_000)
            .When(x => x.InitialValue is not null);
    }
}
```

---

## Response DTOs

Live in the same folder as their domain entity. The static `From` factory is the only mapping point.

```csharp
// MicCheck.Api/Features/FeatureResponse.cs
public record FeatureResponse(
    int Id,
    string Name,
    string Type,
    string? InitialValue,
    string? Description,
    bool DefaultEnabled,
    DateTimeOffset CreatedAt
)
{
    public static FeatureResponse From(Feature feature) => new(
        feature.Id, feature.Name,
        feature.Type.ToString().ToUpperInvariant(),
        feature.InitialValue, feature.Description,
        feature.DefaultEnabled, feature.CreatedAt);
}
```

Pagination wrapper for list endpoints:
```csharp
// MicCheck.Api/PaginatedResponse.cs
public record PaginatedResponse<T>(
    int Count,
    string? Next,
    string? Previous,
    IReadOnlyList<T> Results
);
```

---

## Environment Cloning

When cloning an environment:
1. Create new `Environment` record with a fresh `ApiKey`
2. Copy all environment-level `FeatureState` records from the source environment
3. Do NOT copy identity-level overrides
4. Do NOT copy segment overrides (those follow segment definitions)
5. Emit an audit log entry

---

## Acceptance Criteria
- [ ] Full CRUD for organizations, projects, environments, features, segments
- [ ] Creating a feature auto-creates `FeatureState` for all project environments
- [ ] Creating an environment auto-creates `FeatureState` for all project features
- [ ] 400 feature limit enforced (returns 400 with descriptive error)
- [ ] 100 segment limit enforced
- [ ] 100 segment condition limit enforced
- [ ] FluentValidation errors return 422 with field-level detail
- [ ] Environment clone creates independent copy of feature states
- [ ] All endpoints return 401 for missing credentials, 403 for insufficient permissions
- [ ] Paginated list endpoints default to page size 20, max 100
- [ ] Controllers never call `_db` directly — all data access goes through a service
- [ ] Services never reference request/response DTO types
- [ ] Unit tests for all service methods covering success paths and constraint violations
