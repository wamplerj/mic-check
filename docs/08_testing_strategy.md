# Step 8: Testing Strategy

## Goal
Establish a comprehensive testing strategy that achieves high coverage of business logic, enforces BDD-style test naming, and validates the full request pipeline without touching external resources (database, file system, network).

---

## Test Project Structure

```
tests/
├── MicCheck.Core.Tests/
│   ├── Features/
│   │   ├── FeatureEvaluationServiceTests.cs
│   │   └── FeatureServiceTests.cs
│   ├── Segments/
│   │   └── SegmentEvaluatorTests.cs
│   ├── Environments/
│   │   └── EnvironmentServiceTests.cs
│   ├── Identities/
│   │   └── IdentityResolutionServiceTests.cs
│   ├── Audit/
│   │   └── AuditServiceTests.cs
│   └── Webhooks/
│       ├── WebhookDispatcherTests.cs
│       └── WebhookSignatureTests.cs
├── MicCheck.Api.Tests/
│   ├── Flags/
│   │   ├── FlagsControllerTests.cs
│   │   └── IdentitiesControllerTests.cs
│   ├── Admin/
│   │   ├── FeaturesControllerTests.cs
│   │   ├── EnvironmentsControllerTests.cs
│   │   ├── SegmentsControllerTests.cs
│   │   └── OrganizationsControllerTests.cs
│   └── Authentication/
│       ├── EnvironmentKeyAuthenticationHandlerTests.cs
│       └── ApiKeyAuthenticationHandlerTests.cs
└── MicCheck.Infrastructure.Tests/
    └── (integration tests — marked [Category("Integration")])
```

---

## Naming Convention (BDD Style)

```
WhenA{Subject}_{Condition}_Then{ExpectedOutcome}

Examples:
  WhenAFeatureIsCreated_AndProjectHasReachedMaxFeatures_ThenAnExceptionIsThrown
  WhenAnIdentityIsResolved_WithMatchingSegment_ThenSegmentFlagOverrideIsReturned
  WhenAWebhookIsDispatched_WithASecret_ThenPayloadIsSignedWithHmacSha256
  WhenAnEnvironmentKeyIsInvalid_ThenAuthenticationFails
  WhenAFlagIsUpdated_ThenAnAuditLogEntryIsCreated
```

---

## Test Patterns

### Unit Tests (Api — Services)

Services depend on `MicCheckDbContext`. Use an in-memory SQLite EF Core provider (or a real test-scoped PostgreSQL instance for integration tests) rather than mocking the DbContext. Mock only non-EF dependencies such as `AuditService` or `WebhookQueue`.

```csharp
[TestFixture]
public class FeatureServiceTests
{
    private MicCheckDbContext _db = null!;
    private Mock<AuditService> _auditService = null!;
    private FeatureService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new MicCheckDbContext(options);
        _auditService = new Mock<AuditService>();
        _sut = new FeatureService(_db, _auditService.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void WhenAFeatureIsCreated_AndProjectHasReachedMaxFeatures_ThenAnExceptionIsThrown()
    {
        var project = new Project { Name = "Test", OrganizationId = 1 };
        _db.Projects.Add(project);
        for (var i = 0; i < 400; i++)
            _db.Features.Add(new Feature { Name = $"flag_{i}", ProjectId = project.Id });
        _db.SaveChanges();

        Assert.That(
            async () => await _sut.CreateAsync(project.Id, "new_flag", FeatureType.Standard, null, null),
            Throws.TypeOf<DomainException>());
    }
}
```

### API Controller Tests

Use `WebApplicationFactory<Program>` with mocked services for HTTP-level tests:

```csharp
[TestFixture]
public class FlagsControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IFeatureEvaluationService>(
                    Mock.Of<IFeatureEvaluationService>());
            }));
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task WhenGetFlagsIsCalledWithAValidEnvironmentKey_ThenFlagsAreReturned()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Environment-Key", "valid-env-key");

        var response = await client.GetAsync("/api/v1/flags/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
```

---

## SegmentEvaluator Test Coverage

The `SegmentEvaluator` must have exhaustive unit tests for every operator:

The `SegmentEvaluator` takes no dependencies, so it is constructed directly with no mocking needed.

```csharp
[TestFixture]
public class SegmentEvaluatorTests
{
    private readonly SegmentEvaluator _sut = new();

    [TestCase("premium", "premium", true)]
    [TestCase("premium", "free", false)]
    public void WhenEvaluatingEqualOperator_ThenResultMatchesExpectation(
        string traitValue, string conditionValue, bool expected) { ... }

    [Test]
    public void WhenEvaluatingPercentageSplit_WithSameIdentifier_ThenResultIsDeterministic() { ... }

    [Test]
    public void WhenEvaluatingPercentageSplit_WithZeroPercent_ThenNobodyMatches() { ... }

    [Test]
    public void WhenEvaluatingPercentageSplit_WithHundredPercent_ThenEveryoneMatches() { ... }

    [Test]
    public void WhenASegmentHasNestedRules_ThenAllRulesAreEvaluated() { ... }
}
```

---

## Feature Evaluation Priority Tests

```csharp
[TestFixture]
public class FeatureEvaluationServiceTests
{
    [Test]
    public async Task WhenIdentityHasAnOverride_ThenIdentityOverrideTakesPriorityOverSegment() { ... }

    [Test]
    public async Task WhenIdentityMatchesSegment_ThenSegmentOverrideTakesPriorityOverEnvironmentDefault() { ... }

    [Test]
    public async Task WhenIdentityMatchesMultipleSegments_ThenHighestPrioritySegmentWins() { ... }

    [Test]
    public async Task WhenIdentityMatchesNoSegment_ThenEnvironmentDefaultIsReturned() { ... }
}
```

---

## Authentication Tests

```csharp
[TestFixture]
public class EnvironmentKeyAuthenticationHandlerTests
{
    [Test]
    public async Task WhenEnvironmentKeyHeaderIsMissing_ThenAuthenticationFails() { ... }

    [Test]
    public async Task WhenEnvironmentKeyIsInvalid_ThenAuthenticationFails() { ... }

    [Test]
    public async Task WhenEnvironmentKeyIsValid_ThenAuthenticationSucceedsWithEnvironmentIdClaim() { ... }
}

[TestFixture]
public class ApiKeyAuthenticationHandlerTests
{
    [Test]
    public async Task WhenApiKeyIsRevoked_ThenAuthenticationFails() { ... }

    [Test]
    public async Task WhenApiKeyIsExpired_ThenAuthenticationFails() { ... }

    [Test]
    public async Task WhenApiKeyIsValid_ThenAuthenticationSucceedsWithOrganizationIdClaim() { ... }
}
```

---

## Domain Constraint Tests

```csharp
// For each enforced constraint, there must be a test:
WhenAFeatureIsCreated_AndProjectHasReachedMaxFeatures_ThenAnExceptionIsThrown
WhenASegmentIsCreated_AndProjectHasReachedMaxSegments_ThenAnExceptionIsThrown
WhenASegmentConditionIsAdded_AndRuleHasReachedMaxConditions_ThenAnExceptionIsThrown
WhenAFeatureStateValueExceedsMaxBytes_ThenValidationFails
WhenATraitValueExceedsMaxBytes_ThenValidationFails
```

---

## Code Coverage Target

- `MicCheck.Api` services and domain logic: 90%+ line coverage
- `MicCheck.Api` controllers and auth handlers: 80%+ line coverage
- All new code must include corresponding tests per CLAUDE.md requirements

Run coverage via:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## CI Test Execution

Tests are separated by NUnit category:
```bash
# Run all unit tests (fast, no external dependencies):
dotnet test --filter "Category!=Integration"

# Run integration tests (requires database):
dotnet test --filter "Category=Integration"
```

---

## Acceptance Criteria
- [ ] All tests use BDD-style names (`WhenA{Subject}_{Condition}_Then{Outcome}`)
- [ ] No mocked objects named with the word "Mock" (use descriptive names or `_sut`)
- [ ] No Arrange/Act/Assert comments in test code
- [ ] `SegmentEvaluator` has tests for every `SegmentConditionOperator` value
- [ ] Feature evaluation priority (identity > segment > environment) is tested explicitly
- [ ] All domain constraint limits have corresponding tests
- [ ] `dotnet test` passes cleanly with zero failures
- [ ] Core project achieves 90%+ line coverage
