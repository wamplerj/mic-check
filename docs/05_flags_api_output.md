# Step 5 Output: Flags API

## Summary

Implemented the public Flags API — the SDK-facing endpoints authenticated via `X-Environment-Key`. Includes feature flag evaluation with identity/segment/environment priority, identity resolution, and environment document for local evaluation mode.

---

## What Was Built

### Segment Evaluator (`MicCheck.Api/Segments/SegmentEvaluator.cs`)

Pure stateless logic — no DB access. Evaluates a `Segment` against an identity's traits.

**Rule evaluation:**
- Top-level rules (where `ParentRuleId == null`) are AND-ed together
- Each rule applies its `Type` (All/Any/None) to its `Conditions`
- `ChildRules` are recursively evaluated and AND-ed after conditions

**All 17 operators implemented:**

| Category | Operators |
|---|---|
| String | Equal, NotEqual, Contains, NotContains, Regex |
| Numeric | GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual |
| Boolean | IsTrue, IsFalse |
| Membership | In, NotIn |
| Existence | IsSet, IsNotSet |
| Sampling | PercentageSplit (MD5 hash of `identifier + segmentId`, bucket out of 9999) |
| Modulo | ModuloValueDivisorRemainder (value format: `"divisor|remainder"`) |

### Feature Evaluation Service (`MicCheck.Api/Features/FeatureEvaluationService.cs`)

**Priority order (highest wins):**
1. Identity-level override (`FeatureState` with `IdentityId` set)
2. Segment override — first matching `FeatureSegment` by `Priority` (lowest number first)
3. Environment default (`FeatureState` with no `IdentityId` or `FeatureSegmentId`)

`EvaluateForEnvironmentAsync` — checks `FlagCache` first, loads default feature states via a join query, caches the result.

`EvaluateForIdentityAsync` — loads all feature states + feature segments + segments in a few queries, resolves identity via `IdentityResolutionService`, then evaluates each feature in a single in-memory pass.

### Flag Cache (`MicCheck.Api/Features/FlagCache.cs`)

`IMemoryCache` wrapper with 60-second TTL, keyed by `environmentId`. Registered as a singleton. Interface designed to swap backing store for Redis without changing callers.

### Identity Resolution Service (`MicCheck.Api/Identities/IdentityResolutionService.cs`)

1. Looks up `Identity` by `(EnvironmentId, Identifier)` — creates if not found
2. Upserts provided traits by key (updates value and type if key exists, inserts otherwise)
3. Returns the resolved `Identity` with current traits loaded

### TraitInput (`MicCheck.Api/Identities/TraitInput.cs`)

Record with `[JsonPropertyName]` attributes for snake_case API compatibility (`trait_key`, `trait_value`). Handles `JsonElement` deserialization from `System.Text.Json` to determine the correct `TraitValueType`.

### Endpoints

All require `FlagsApiAccess` policy (`X-Environment-Key` header).

| Method | Path | Controller |
|---|---|---|
| GET | `/api/v1/flags/` | `FlagsController` |
| GET | `/api/v1/identities/?identifier=` | `IdentitiesController` |
| POST | `/api/v1/identities/` | `IdentitiesController` |
| GET | `/api/v1/environment-document/` | `EnvironmentDocumentController` |

### Response Types

- `FlagResponse(Id, Feature, Enabled, FeatureStateValue)` — static `From(FeatureStateResult)` factory
- `FeatureSummaryResponse(Id, Name, Type)` — type is uppercased (`STANDARD`, `MULTI_VARIATE`)
- `IdentityResponse(Traits, Flags)`
- `TraitResponse(TraitKey, TraitValue)`
- `EnvironmentDocumentResponse` — full environment config including project segments for local evaluation mode

All response objects are constructed in controller action methods only; services never return or accept DTOs.

---

## Deviations from Plan

- `SegmentEvaluator.Evaluate` takes an optional `identifier` parameter (required for `PercentageSplit`)
- `FeatureEvaluationService.EvaluateForIdentityAsync` creates an `IdentityResolutionService` instance internally rather than injecting it (avoids double-inject of scoped service that's also used by the controller directly)
- **`FeatureSegmentConfiguration` bug fixed**: The existing configuration had `HasForeignKey<FeatureSegment>(fsg => fsg.Id)` (FK on `FeatureSegment.Id` → `FeatureState.Id`, shared-PK pattern), which is incorrect for the data model. Changed to `HasForeignKey<FeatureState>(fs => fs.FeatureSegmentId)` so `FeatureState.FeatureSegmentId` references `FeatureSegment.Id` as intended.

---

## Tests Added

### `SegmentEvaluatorTests` (27 tests)

Covers all 17 operators plus rule type logic:
- Equal, NotEqual (case-insensitive)
- GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual
- Contains, NotContains (case-insensitive)
- Regex match/no-match
- IsSet/IsNotSet (trait existence)
- In/NotIn (comma-separated list)
- IsTrue, IsFalse
- ModuloValueDivisorRemainder satisfied/not satisfied
- PercentageSplit is deterministic for same input
- PercentageSplit at 100% includes all identities
- PercentageSplit at 0% includes no identities
- Rule type Any (at least one must match)
- Rule type None (none must match / one match → fail)
- Multiple top-level rules AND-ed together

### `FeatureEvaluationServiceTests` (7 tests, in-memory DB)

- Environment default returned when no overrides exist
- Disabled flag reflected correctly
- Identity with no overrides → environment default
- Identity override takes priority over environment default
- Segment override used when segment matches
- Environment default used when segment doesn't match
- Identity override takes priority over matching segment override

### `FlagsApiIntegrationTests` (5 tests, WebApplicationFactory)

- Valid environment key → 200 with correct flags
- Missing environment key → 401
- Invalid environment key → 401
- POST identities with traits → 200
- GET environment document → 200

---

## Issues Encountered

1. **`FeatureSegmentConfiguration` FK was backwards** — the stored configuration put the FK on `FeatureSegment.Id` (sharing the PK with `FeatureState`), which made it impossible to create a `FeatureSegment` before a matching `FeatureState` existed. Fixed to use `FeatureState.FeatureSegmentId` as the FK.

2. **`TraitInput` JSON naming** — The plan's API spec uses `trait_key` / `trait_value` (snake_case). ASP.NET Core's default `System.Text.Json` serializer does case-insensitive camelCase matching but not snake_case. Added `[JsonPropertyName]` attributes to `TraitInput`.

3. **Integration test seeding** — `EnvironmentDocumentController` loads the project from DB. The initial integration test seeded an environment but not a project, causing a 404. Fixed by seeding a project first.

---

## Test Results

```
Passed! - Failed: 0, Passed: 142, Skipped: 0, Total: 142
```
