# Step 4: Authentication & Authorization

## Goal
Implement the two authentication schemes Flagsmith uses — Environment Key (public, for Flags API) and API Key (secret, for Admin API) — plus JWT for dashboard users, and role-based authorization.

---

## Authentication Schemes

### 1. Environment Key (Flags API)

- Delivered via `X-Environment-Key` header
- Public — safe for client-side exposure
- Identifies which environment to serve flags for
- No user identity implied

**Implementation:**
```csharp
// MicCheck.Api/Authentication/EnvironmentKeyAuthenticationHandler.cs
public class EnvironmentKeyAuthenticationHandler 
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // Reads X-Environment-Key header
    // Looks up Environment from DB by ApiKey value
    // Sets ClaimsPrincipal with EnvironmentId claim
    // Scheme name: "EnvironmentKey"
}
```

### 2. Organisation API Key (Admin API)

- Delivered via `Authorization: Api-Key <TOKEN>` header
- Secret — never expose client-side
- Identifies the organization and grants admin access
- Stored hashed in DB; prefix shown for identification

**Implementation:**
```csharp
// MicCheck.Api/Authentication/ApiKeyAuthenticationHandler.cs
public class ApiKeyAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // Parses "Api-Key <TOKEN>" from Authorization header
    // Hashes the provided token
    // Looks up ApiKey record by hash
    // Validates IsActive and ExpiresAt
    // Sets ClaimsPrincipal with OrganizationId and UserId claims
    // Scheme name: "ApiKey"
}
```

**API key hashing:**
- Use `SHA256` for hashing stored keys
- Store prefix (first 8 chars) for display/identification only
- Generate new keys with `RandomNumberGenerator.GetBytes(32)` → Base64URL encoded

### 3. JWT (Dashboard Users)

- Standard bearer token authentication
- Used for the admin web UI
- Short-lived access token + refresh token pattern

**Implementation:**
```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options => {
        options.TokenValidationParameters = new() {
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Secret"]!))
        };
    });
```

---

## Authorization Policies

Define named policies in `MicCheck.Api/Authorization/`:

```csharp
// Policies
public static class AuthorizationPolicies
{
    public const string FlagsApiAccess = "FlagsApiAccess";
    public const string AdminApiAccess = "AdminApiAccess";
    public const string OrganizationAdmin = "OrganizationAdmin";
}
```

**Policy registrations:**
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.FlagsApiAccess, policy =>
        policy.AddAuthenticationSchemes("EnvironmentKey")
              .RequireClaim("EnvironmentId"));

    options.AddPolicy(AuthorizationPolicies.AdminApiAccess, policy =>
        policy.AddAuthenticationSchemes("ApiKey", "Bearer")
              .RequireAuthenticatedUser());

    options.AddPolicy(AuthorizationPolicies.OrganizationAdmin, policy =>
        policy.AddAuthenticationSchemes("ApiKey", "Bearer")
              .RequireClaim("OrganizationRole", "Admin"));
});
```

---

## Role-Based Access Control (RBAC)

### Organization Roles
- `Admin` — full access to all projects and settings
- `User` — access to assigned projects only

### Project-Level Permissions

```csharp
// MicCheck.Api/Authorization/ProjectPermission.cs
public enum ProjectPermission
{
    ViewProject,
    CreateFeature, EditFeature, DeleteFeature,
    CreateEnvironment, EditEnvironment, DeleteEnvironment,
    CreateSegment, EditSegment, DeleteSegment,
    ManageWebhooks,
    ViewAuditLog
}

// MicCheck.Api/Authorization/UserProjectPermission.cs
public class UserProjectPermission
{
    public int UserId { get; init; }
    public int ProjectId { get; init; }
    public ICollection<ProjectPermission> Permissions { get; init; } = [];
    public bool IsAdmin { get; set; } // grants all permissions
}
```

### Authorization requirement handler:

```csharp
// MicCheck.Api/Authorization/ProjectPermissionRequirementHandler.cs
// Checks UserProjectPermission for the current user and requested project
// Organization admins bypass all project-level checks
```

---

## User Model

```csharp
// MicCheck.Api/Users/User.cs
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

**Password hashing:** Use ASP.NET Core's `IPasswordHasher<User>`.

---

## Auth Endpoints

```
POST /api/v1/auth/login          → Returns JWT access + refresh tokens
POST /api/v1/auth/refresh        → Exchanges refresh token for new access token
POST /api/v1/auth/logout         → Revokes refresh token
POST /api/v1/auth/register       → Creates new user + organization

POST /api/v1/organisations/{id}/api-keys/   → Create new API key
GET  /api/v1/organisations/{id}/api-keys/   → List API keys (prefix only)
DELETE /api/v1/organisations/{id}/api-keys/{keyId}/ → Revoke key
```

---

## Rate Limiting

Apply to Admin API:
- 500 requests per minute per API key
- Use ASP.NET Core's built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`)

```csharp
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("AdminApi", limiter => {
        limiter.PermitLimit = 500;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    }));
```

---

## Acceptance Criteria
- [ ] `X-Environment-Key` header resolves to correct environment; invalid key returns 401
- [ ] `Authorization: Api-Key <TOKEN>` validates against hashed DB record; invalid key returns 401
- [ ] JWT tokens are issued and validated correctly
- [ ] Organization admins have access to all project operations
- [ ] Project-level permission checks block unauthorized users with 403
- [ ] Rate limit returns 429 after 500 Admin API requests within a minute
- [ ] Unit tests cover: key hashing, auth handler success/failure paths, permission checks
