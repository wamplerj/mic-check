# Step 1: Project Setup & Solution Structure

## Goal
Establish the solution structure, project references, NuGet packages, and foundational infrastructure for the MicCheck C# port of Flagsmith.

## Solution Layout

```
mic-check.sln
├── src/
│   ├── MicCheck.Api/                  # ASP.NET Core Web API — domain, services, and controllers
│   │   ├── Features/                  # Feature flag domain, services, controllers, DTOs
│   │   ├── Environments/              # Environment domain, services, controllers, DTOs
│   │   ├── Projects/                  # Project domain, services, controllers, DTOs
│   │   ├── Organizations/             # Organization domain, services, controllers, DTOs
│   │   ├── Segments/                  # Segment domain, services, controllers, DTOs
│   │   ├── Identities/               # Identity domain, services, controllers, DTOs
│   │   ├── Audit/                     # Audit log domain, service, controller, DTOs
│   │   ├── Webhooks/                  # Webhook domain, service, controller, DTOs
│   │   ├── ApiKeys/                   # API key domain, service, DTOs
│   │   └── Authentication/            # Auth handlers and policies
│   └── MicCheck.Infrastructure/       # EF Core DbContext, entity configurations, migrations
└── tests/
    ├── MicCheck.Api.Tests/
    └── MicCheck.Infrastructure.Tests/
```

Each feature folder in `MicCheck.Api` contains everything for that area:
- Domain model(s) — e.g. `Feature.cs`, `FeatureState.cs`
- Service(s) — e.g. `FeatureService.cs`
- Controller(s) — e.g. `FeaturesController.cs`
- Request/Response DTOs — e.g. `CreateFeatureRequest.cs`, `FeatureResponse.cs`

## Project Configurations

### All `.csproj` files must include:
```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

## MicCheck.Api

**Purpose:** The entire application — domain models, business logic services, and HTTP controllers all live here, organized by feature area.

**NuGet packages:**
- `Microsoft.AspNetCore.Authentication.JwtBearer` (latest)
- `Swashbuckle.AspNetCore` (latest)
- `Serilog.AspNetCore` (latest)
- `FluentValidation.AspNetCore` (latest)

**Project reference:**
- References `MicCheck.Infrastructure`

## MicCheck.Infrastructure

**Purpose:** EF Core DbContext, entity configurations, and PostgreSQL migrations only. No business logic.

**NuGet packages:**
- `Microsoft.EntityFrameworkCore` (latest for net10.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (latest)
- `Microsoft.EntityFrameworkCore.Design` (latest)

**Key namespaces:**
- `MicCheck.Infrastructure.Data` — DbContext, entity configurations, migrations

## Test Projects

**NuGet packages (all test projects):**
- `NUnit` (latest)
- `NUnit3TestAdapter` (latest)
- `Moq` (latest)
- `Microsoft.NET.Test.Sdk` (latest)
- `coverlet.collector` (latest)

**Project references:**
- `MicCheck.Api.Tests` references `MicCheck.Api` and `MicCheck.Infrastructure`
- `MicCheck.Infrastructure.Tests` references `MicCheck.Infrastructure`

## Docker Compose (development)

Services required:
- `postgres:15-alpine` — primary database, port 5432
- `mic-check-api` — the ASP.NET Core API, port 8080

## Environment Variables

```
DATABASE_URL=postgresql://miccheck:password@localhost:5432/miccheck
JWT_SECRET=<secret>
ADMIN_API_KEY=<key>
```

## Acceptance Criteria
- [ ] Solution builds with `dotnet build` with zero warnings
- [ ] All projects target net10.0 with `LangVersion=latest` and `Nullable=enable`
- [ ] Docker Compose brings up API and database cleanly
- [ ] `dotnet test` runs successfully (no tests yet, but harness works)
- [ ] Swagger UI is accessible at `/swagger`
