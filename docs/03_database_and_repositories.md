# Step 3: Database Schema & EF Core

## Goal
Implement the EF Core `DbContext` and entity configurations in `MicCheck.Infrastructure`, and wire up services in `MicCheck.Api` to use it directly. There is no repository abstraction layer — services inject `MicCheckDbContext` and query it with LINQ.

---

## DbContext

```csharp
// MicCheck.Infrastructure/Data/MicCheckDbContext.cs
public class MicCheckDbContext : DbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Environment> Environments => Set<Environment>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<FeatureState> FeatureStates => Set<FeatureState>();
    public DbSet<FeatureSegment> FeatureSegments => Set<FeatureSegment>();
    public DbSet<Segment> Segments => Set<Segment>();
    public DbSet<SegmentRule> SegmentRules => Set<SegmentRule>();
    public DbSet<SegmentCondition> SegmentConditions => Set<SegmentCondition>();
    public DbSet<Identity> Identities => Set<Identity>();
    public DbSet<IdentityTrait> IdentityTraits => Set<IdentityTrait>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MicCheckDbContext).Assembly);
}
```

---

## Entity Configurations (EF Core Fluent API)

Each domain entity gets its own `IEntityTypeConfiguration<T>` in `MicCheck.Infrastructure/Data/Configurations/`:

### Key configurations:

**Feature:**
```csharp
entity.Property(f => f.Name).HasMaxLength(150).IsRequired();
entity.Property(f => f.InitialValue).HasMaxLength(20_000);
entity.HasIndex(f => new { f.ProjectId, f.Name }).IsUnique();
```

**FeatureState:**
```csharp
entity.Property(fs => fs.Value).HasMaxLength(20_000);
entity.Property(fs => fs.Version).IsConcurrencyToken();
entity.HasIndex(fs => new { fs.FeatureId, fs.EnvironmentId, fs.IdentityId }).IsUnique();
```

**IdentityTrait:**
```csharp
entity.Property(t => t.Value).HasMaxLength(2_000);
entity.HasIndex(t => new { t.IdentityId, t.Key }).IsUnique();
```

**Environment:**
```csharp
entity.Property(e => e.ApiKey).HasMaxLength(100).IsRequired();
entity.HasIndex(e => e.ApiKey).IsUnique();
```

**ApiKey:**
```csharp
entity.Property(k => k.Key).HasMaxLength(512).IsRequired();  // stores hash
entity.Property(k => k.Prefix).HasMaxLength(8).IsRequired();
entity.HasIndex(k => k.Key).IsUnique();
```

---

## Service Usage Pattern

Services receive `MicCheckDbContext` via constructor injection and query it directly:

```csharp
// MicCheck.Api/Features/FeatureService.cs
public class FeatureService
{
    private readonly MicCheckDbContext _db;
    private readonly AuditService _auditService;

    public FeatureService(MicCheckDbContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task<Feature> CreateAsync(
        int projectId, string name, FeatureType type, string? initialValue,
        CancellationToken ct = default)
    {
        var count = await _db.Features.CountAsync(f => f.ProjectId == projectId, ct);
        if (count >= 400)
            throw new DomainException("Projects may not have more than 400 features.");

        var feature = new Feature { ProjectId = projectId, Name = name, Type = type, InitialValue = initialValue };
        _db.Features.Add(feature);

        // Auto-create FeatureState for every existing environment
        var environments = await _db.Environments
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(ct);

        foreach (var env in environments)
            _db.FeatureStates.Add(new FeatureState { FeatureId = feature.Id, EnvironmentId = env.Id });

        await _db.SaveChangesAsync(ct);

        await _auditService.RecordAsync("Feature", feature.Id.ToString(), "Created", ...);
        return feature;
    }
}
```

Controllers call the service and map the returned domain object to a response DTO:

```csharp
// MicCheck.Api/Features/FeaturesController.cs
[HttpPost]
public async Task<ActionResult<FeatureResponse>> Create(
    int projectId, CreateFeatureRequest request, CancellationToken ct)
{
    var feature = await _featureService.CreateAsync(
        projectId, request.Name, request.Type, request.InitialValue, ct);

    return CreatedAtAction(nameof(GetById), new { projectId, id = feature.Id },
        FeatureResponse.From(feature));
}
```

The static `From` method (or a mapping method on the controller) is the only place a domain object becomes a DTO.

---

## Migrations

- Use EF Core code-first migrations
- Migration naming: `{Timestamp}_{DescriptiveName}` (e.g., `20260101_InitialSchema`)
- All migrations in `MicCheck.Infrastructure/Data/Migrations/`
- Run via `dotnet ef migrations add` and `dotnet ef database update`
- The `--project MicCheck.Infrastructure --startup-project MicCheck.Api` flags are required since the DbContext is in Infrastructure

---

## Connection String Configuration

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=miccheck;Username=miccheck;Password=password"
  }
}
```

Support `DATABASE_URL` environment variable override (Heroku/Railway style parsing).

---

## Seed Data

Create an initial migration seeder for local development:
- Default organization: "Default"
- Default project: "My Project"
- Default environments: "Development", "Staging", "Production"

---

## Indexes for Performance

Critical indexes beyond unique constraints:
- `FeatureState (EnvironmentId)` — for flag serving
- `FeatureState (IdentityId)` — for identity lookups
- `Identity (EnvironmentId, Identifier)` — unique, for identity resolution
- `AuditLog (OrganizationId, CreatedAt DESC)` — for audit queries
- `Webhook (EnvironmentId)` — for event dispatch

---

## Acceptance Criteria
- [ ] `dotnet ef migrations add InitialSchema` generates a valid migration
- [ ] `dotnet ef database update` applies cleanly against a fresh PostgreSQL instance
- [ ] All unique constraints are enforced at the database level
- [ ] Services use `MicCheckDbContext` directly — no repository interfaces
- [ ] No raw SQL — all queries use EF Core LINQ
- [ ] Integration tests against a real test database verify EF configurations are correct
