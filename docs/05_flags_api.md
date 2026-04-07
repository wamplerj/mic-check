# Step 5: Flags API (Public SDK Endpoint)

## Goal
Implement the public Flags API — the endpoints consumed by SDK clients to retrieve feature flags and identify users. This is the high-traffic, read-heavy API authenticated via `X-Environment-Key`.

---

## Endpoints

All routes prefixed with `/api/v1/` and authenticated with the `EnvironmentKey` scheme.

### GET /api/v1/flags/
Returns all enabled feature flags for the resolved environment.

**Response:**
```json
[
  {
    "id": 1,
    "feature": {
      "id": 1,
      "name": "dark_mode",
      "type": "STANDARD"
    },
    "enabled": true,
    "feature_state_value": null
  }
]
```

### GET /api/v1/identities/?identifier=user123
Returns flags for a specific user identity, including segment overrides and identity-level overrides.

### POST /api/v1/identities/
Identifies a user, optionally sets traits, and returns flags.

**Request:**
```json
{
  "identifier": "user123",
  "traits": [
    { "trait_key": "plan", "trait_value": "premium" }
  ]
}
```

**Response:**
```json
{
  "traits": [...],
  "flags": [...]
}
```

### GET /api/v1/environment-document/
Returns the full environment configuration for **local evaluation mode**.
Used by server-side SDKs to pull all flags, segments, and targeting rules locally.

**Response:**
```json
{
  "id": 1,
  "api_key": "env-key-xxx",
  "feature_states": [...],
  "project": {
    "id": 1,
    "name": "My Project",
    "segments": [...]
  }
}
```

---

## Feature Evaluation Service

Lives in `MicCheck.Api/Features/`. Injects `MicCheckDbContext` and returns domain objects — no DTOs.

```csharp
// MicCheck.Api/Features/FeatureEvaluationService.cs
public class FeatureEvaluationService
{
    private readonly MicCheckDbContext _db;
    private readonly SegmentEvaluator _segmentEvaluator;
    private readonly FlagCache _flagCache;

    // Resolves the effective FeatureState for a given environment
    // Priority order (highest wins):
    //   1. Identity-level override
    //   2. Segment override (highest priority segment that matches)
    //   3. Environment default

    public Task<IReadOnlyList<FeatureStateResult>> EvaluateForEnvironmentAsync(
        int environmentId, CancellationToken ct = default);

    public Task<IReadOnlyList<FeatureStateResult>> EvaluateForIdentityAsync(
        int environmentId,
        string identifier,
        IReadOnlyList<TraitInput>? traits = null,
        CancellationToken ct = default);
}
```

---

## Identity Resolution Service

Lives in `MicCheck.Api/Identities/`. Injects `MicCheckDbContext` and returns domain objects.

```csharp
// MicCheck.Api/Identities/IdentityResolutionService.cs
public class IdentityResolutionService
{
    private readonly MicCheckDbContext _db;

    // 1. Look up Identity by (EnvironmentId, Identifier) — create if not found
    // 2. Upsert provided traits by key
    // 3. Return the resolved Identity with current traits loaded
    public Task<Identity> ResolveAsync(
        int environmentId,
        string identifier,
        IReadOnlyList<TraitInput>? traits,
        CancellationToken ct = default);
}
```

---

## Segment Evaluator

Lives in `MicCheck.Api/Segments/`. Pure logic — no DB access (data is preloaded by callers).

```csharp
// MicCheck.Api/Segments/SegmentEvaluator.cs
public class SegmentEvaluator
{
    public bool Evaluate(Segment segment, IReadOnlyList<IdentityTrait> traits);

    private bool EvaluateRule(SegmentRule rule, IReadOnlyList<IdentityTrait> traits);
    private bool EvaluateCondition(SegmentCondition condition, IReadOnlyList<IdentityTrait> traits);
}
```

**Operator implementations** (pure functions):
- String: Equal, NotEqual, Contains, NotContains, Regex
- Numeric: GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual
- Boolean: IsTrue, IsFalse
- Membership: In, NotIn
- Existence: IsSet, IsNotSet
- Sampling: PercentageSplit (deterministic by identity+segment hash)
- Modulo: ModuloValueDivisorRemainder

---

## Request/Response DTOs

Live alongside their controllers in the relevant feature folder. DTOs are only constructed in controller action methods — services never return or accept DTOs.

```csharp
// Features/FlagResponse.cs
public record FlagResponse(
    int Id,
    FeatureSummaryResponse Feature,
    bool Enabled,
    string? FeatureStateValue
)
{
    public static FlagResponse From(FeatureStateResult result) => new(
        result.Feature.Id,  // mapping happens here, in the response type, called from the controller
        FeatureSummaryResponse.From(result.Feature),
        result.Enabled,
        result.Value);
}

// Features/FeatureSummaryResponse.cs
public record FeatureSummaryResponse(int Id, string Name, string Type)
{
    public static FeatureSummaryResponse From(Feature feature) =>
        new(feature.Id, feature.Name, feature.Type.ToString().ToUpperInvariant());
}

// Identities/IdentityRequest.cs
public record IdentityRequest(
    string Identifier,
    IReadOnlyList<TraitInput>? Traits
);

// Identities/TraitInput.cs
public record TraitInput(
    string TraitKey,
    object? TraitValue   // string | int | float | bool
);
```

---

## Controllers

Controllers receive a request DTO, call a service with domain-level parameters, then map the domain result to a response DTO before returning.

```csharp
// MicCheck.Api/Features/FlagsController.cs
[ApiController]
[Route("api/v1/flags")]
[Authorize(Policy = AuthorizationPolicies.FlagsApiAccess)]
public class FlagsController : ControllerBase
{
    private readonly FeatureEvaluationService _featureEvaluationService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlagResponse>>> GetAll(CancellationToken ct)
    {
        var environmentId = int.Parse(User.FindFirstValue("EnvironmentId")!);
        var results = await _featureEvaluationService.EvaluateForEnvironmentAsync(environmentId, ct);
        return Ok(results.Select(FlagResponse.From).ToList());
    }
}

// MicCheck.Api/Identities/IdentitiesController.cs
[ApiController]
[Route("api/v1/identities")]
[Authorize(Policy = AuthorizationPolicies.FlagsApiAccess)]
public class IdentitiesController : ControllerBase { ... }

// MicCheck.Api/Environments/EnvironmentDocumentController.cs
[ApiController]
[Route("api/v1/environment-document")]
[Authorize(Policy = AuthorizationPolicies.FlagsApiAccess)]
public class EnvironmentDocumentController : ControllerBase { ... }
```

---

## Caching

Lives in `MicCheck.Api/Features/`. Cache stores domain results, not serialized responses.

```csharp
// MicCheck.Api/Features/FlagCache.cs
public class FlagCache
{
    private readonly IMemoryCache _cache;

    public IReadOnlyList<FeatureStateResult>? Get(int environmentId);
    public void Set(int environmentId, IReadOnlyList<FeatureStateResult> flags);
    public void Invalidate(int environmentId);
}
```

Cache is invalidated inside `FeatureEvaluationService` whenever a `FeatureState` is modified. Designed to swap the backing store for Redis without changing the `FlagCache` interface.

---

## Acceptance Criteria
- [ ] `GET /api/v1/flags/` returns correct flags for a valid environment key
- [ ] `GET /api/v1/flags/` returns 401 for missing or invalid environment key
- [ ] `POST /api/v1/identities/` creates identity + upserts traits + returns correct flags
- [ ] Segment override takes priority over environment default
- [ ] Identity override takes priority over segment override
- [ ] `GET /api/v1/environment-document/` returns full environment config
- [ ] Controllers never pass DTOs into services — only primitive/domain values
- [ ] `SegmentEvaluator` unit tests cover all operator types
- [ ] `FeatureEvaluationService` unit tests cover all three priority tiers
- [ ] `PercentageSplit` distributes identities deterministically and uniformly
