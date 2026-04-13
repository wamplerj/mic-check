# Step 2: Domain Models

## Goal
Define all domain entities in `MicCheck.Api`, organized by feature area. Each entity lives in the folder for the feature it belongs to.

## Constraints (from Flagsmith spec)
- Max 400 features per project
- Max 100 segments per project
- Max 100 segment overrides per environment
- Max 100 segment rule conditions
- Feature flag values: max 20,000 bytes
- Identity trait values: max 2,000 bytes

---

## MicCheck.Api/Organizations/

```csharp
// Organizations/Organization.cs
public class Organization
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<Project> Projects { get; init; } = [];
    public ICollection<OrganizationUser> Members { get; init; } = [];
    public ICollection<ApiKey> ApiKeys { get; init; } = [];
}

// Organizations/OrganizationUser.cs
public class OrganizationUser
{
    public int OrganizationId { get; init; }
    public int UserId { get; init; }
    public OrganizationRole Role { get; set; }
}

public enum OrganizationRole { Admin, User }
```

---

## MicCheck.Api/Projects/

```csharp
// Projects/Project.cs
public class Project
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public int OrganizationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool HideDisabledFlags { get; set; }
    public ICollection<Environment> Environments { get; init; } = [];
    public ICollection<Feature> Features { get; init; } = [];
    public ICollection<Segment> Segments { get; init; } = [];
}
```

---

## MicCheck.Api/Environments/

```csharp
// Environments/Environment.cs
public class Environment
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public required string ApiKey { get; set; }   // X-Environment-Key header value
    public int ProjectId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<FeatureState> FeatureStates { get; init; } = [];
}
```

---

## MicCheck.Api/Features/

```csharp
// Features/Feature.cs
public class Feature
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public FeatureType Type { get; set; }
    public string? InitialValue { get; set; }        // max 20,000 bytes
    public string? Description { get; set; }
    public bool DefaultEnabled { get; set; }
    public int ProjectId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<FeatureState> FeatureStates { get; init; } = [];
    public ICollection<Tag> Tags { get; init; } = [];
}

public enum FeatureType { Standard, MultiVariate }

// Features/FeatureState.cs
// Represents the value of a feature within a specific environment
public class FeatureState
{
    public int Id { get; init; }
    public int FeatureId { get; init; }
    public int EnvironmentId { get; init; }
    public int? IdentityId { get; set; }        // null = environment-level
    public bool Enabled { get; set; }
    public string? Value { get; set; }           // max 20,000 bytes
    public int? FeatureSegmentId { get; set; }  // null = not a segment override
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int Version { get; set; }            // optimistic concurrency
}

// Features/FeatureSegment.cs
// Links a feature to a segment with an override priority
public class FeatureSegment
{
    public int Id { get; init; }
    public int FeatureId { get; init; }
    public int SegmentId { get; init; }
    public int EnvironmentId { get; init; }
    public int Priority { get; set; }
    public FeatureState? FeatureState { get; set; }
}

// Features/Tag.cs
public class Tag
{
    public int Id { get; init; }
    public required string Label { get; set; }
    public required string Color { get; set; }
    public int ProjectId { get; init; }
}

// Features/FeatureStateResult.cs
// Value object returned by FeatureEvaluationService — never exposed directly via API
public class FeatureStateResult
{
    public required Feature Feature { get; init; }
    public bool Enabled { get; init; }
    public string? Value { get; init; }
}
```

---

## MicCheck.Api/Segments/

```csharp
// Segments/Segment.cs
public class Segment
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public int ProjectId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<SegmentRule> Rules { get; init; } = [];
}

// Segments/SegmentRule.cs
public class SegmentRule
{
    public int Id { get; init; }
    public int SegmentId { get; init; }
    public int? ParentRuleId { get; set; }   // allows nested AND/OR
    public SegmentRuleType Type { get; set; }
    public ICollection<SegmentCondition> Conditions { get; init; } = [];
    public ICollection<SegmentRule> ChildRules { get; init; } = [];
}

public enum SegmentRuleType { All, Any, None }

// Segments/SegmentCondition.cs
public class SegmentCondition
{
    public int Id { get; init; }
    public int RuleId { get; init; }
    public required string Property { get; set; }   // trait key
    public SegmentConditionOperator Operator { get; set; }
    public required string Value { get; set; }
}

public enum SegmentConditionOperator
{
    Equal, NotEqual,
    GreaterThan, GreaterThanOrEqual,
    LessThan, LessThanOrEqual,
    Contains, NotContains,
    Regex,
    IsSet, IsNotSet,
    In, NotIn,
    PercentageSplit,
    ModuloValueDivisorRemainder,
    IsTrue, IsFalse
}
```

---

## MicCheck.Api/Identities/

```csharp
// Identities/Identity.cs
// Represents an end-user or SDK client
public class Identity
{
    public int Id { get; init; }
    public required string Identifier { get; set; }
    public int EnvironmentId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<IdentityTrait> Traits { get; init; } = [];
    public ICollection<FeatureState> FeatureStateOverrides { get; init; } = [];
}

// Identities/IdentityTrait.cs
public class IdentityTrait
{
    public int Id { get; init; }
    public int IdentityId { get; init; }
    public required string Key { get; set; }
    public required string Value { get; set; }   // max 2,000 bytes
    public TraitValueType ValueType { get; set; }
}

public enum TraitValueType { String, Integer, Float, Boolean }
```

---

## MicCheck.Api/Audit/

```csharp
// Audit/AuditLog.cs
public class AuditLog
{
    public int Id { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public required string Action { get; init; }    // Created, Updated, Deleted
    public string? Changes { get; init; }           // JSON diff
    public int OrganizationId { get; init; }
    public int? ProjectId { get; init; }
    public int? EnvironmentId { get; init; }
    public int? ActorUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

---

## MicCheck.Api/Webhooks/

```csharp
// Webhooks/Webhook.cs
public class Webhook
{
    public int Id { get; init; }
    public required string Url { get; set; }
    public string? Secret { get; set; }
    public WebhookScope Scope { get; set; }
    public int? EnvironmentId { get; set; }
    public int? OrganizationId { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

public enum WebhookScope { Environment, Organization }

// Webhooks/WebhookDeliveryLog.cs
public class WebhookDeliveryLog
{
    public int Id { get; init; }
    public int WebhookId { get; init; }
    public required string EventType { get; init; }
    public required string PayloadJson { get; init; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset AttemptedAt { get; init; }
    public TimeSpan Duration { get; set; }
}
```

---

## MicCheck.Api/ApiKeys/

```csharp
// ApiKeys/ApiKey.cs
public class ApiKey
{
    public int Id { get; init; }
    public required string Key { get; set; }    // hashed
    public required string Prefix { get; set; } // first 8 chars for display
    public required string Name { get; set; }
    public int OrganizationId { get; init; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

---

## MicCheck.Api/Users/ (referenced by auth)

```csharp
// Users/User.cs
public class User
{
    public int Id { get; init; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public ICollection<OrganizationUser> Organizations { get; init; } = [];
}
```

---

## Acceptance Criteria
- [ ] All domain models compile within `MicCheck.Api` with no cross-project domain dependencies
- [ ] Nullable reference types enabled — all required properties explicitly marked
- [ ] No domain logic in DTOs — domain models and request/response types are distinct classes
- [ ] Unit tests validate domain constraints (e.g., feature count limits enforced in service logic)
