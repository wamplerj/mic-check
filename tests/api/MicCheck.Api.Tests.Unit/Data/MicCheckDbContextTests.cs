using NUnit.Framework;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Identities;
using MicCheck.Api.Audit;
using MicCheck.Api.Webhooks;
using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Users;
using Microsoft.EntityFrameworkCore;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Data;

[TestFixture]
public class MicCheckDbContextTests
{
    private MicCheckDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new MicCheckDbContext(options);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task WhenAnOrganizationIsSaved_ThenItCanBeRetrievedById()
    {
        var organization = new Organization { Name = "Acme", CreatedAt = DateTimeOffset.UtcNow };
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync();

        var retrieved = await _db.Organizations.FindAsync(organization.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("Acme"));
    }

    [Test]
    public async Task WhenAProjectIsSaved_ThenItCanBeRetrievedByOrganization()
    {
        var organization = new Organization { Name = "Acme", CreatedAt = DateTimeOffset.UtcNow };
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync();

        var project = new Project { Name = "My Project", OrganizationId = organization.Id, CreatedAt = DateTimeOffset.UtcNow };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var projects = await _db.Projects.Where(p => p.OrganizationId == organization.Id).ToListAsync();

        Assert.That(projects, Has.Count.EqualTo(1));
        Assert.That(projects[0].Name, Is.EqualTo("My Project"));
    }

    [Test]
    public async Task WhenAnEnvironmentIsSaved_ThenItCanBeRetrievedByApiKey()
    {
        var project = new Project { Name = "Test", OrganizationId = 1, CreatedAt = DateTimeOffset.UtcNow };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var env = new AppEnvironment { Name = "Production", ApiKey = "env-key-abc", ProjectId = project.Id, CreatedAt = DateTimeOffset.UtcNow };
        _db.Environments.Add(env);
        await _db.SaveChangesAsync();

        var retrieved = await _db.Environments.FirstOrDefaultAsync(e => e.ApiKey == "env-key-abc");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("Production"));
    }

    [Test]
    public async Task WhenAFeatureIsSaved_ThenItCanBeRetrievedByProject()
    {
        var feature = new Feature { Name = "dark_mode", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        _db.Features.Add(feature);
        await _db.SaveChangesAsync();

        var features = await _db.Features.Where(f => f.ProjectId == 1).ToListAsync();

        Assert.That(features, Has.Count.EqualTo(1));
        Assert.That(features[0].Name, Is.EqualTo("dark_mode"));
    }

    [Test]
    public async Task WhenAFeatureStateIsSaved_ThenItCanBeQueriedByEnvironment()
    {
        var state = new FeatureState
        {
            FeatureId = 1,
            EnvironmentId = 1,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.FeatureStates.Add(state);
        await _db.SaveChangesAsync();

        var states = await _db.FeatureStates.Where(fs => fs.EnvironmentId == 1).ToListAsync();

        Assert.That(states, Has.Count.EqualTo(1));
        Assert.That(states[0].Enabled, Is.True);
    }

    [Test]
    public async Task WhenASegmentIsSaved_ThenItsRulesCanBeLoaded()
    {
        var segment = new Segment { Name = "Power Users", ProjectId = 1, CreatedAt = DateTimeOffset.UtcNow };
        _db.Segments.Add(segment);
        await _db.SaveChangesAsync();

        var rule = new SegmentRule { SegmentId = segment.Id, Type = SegmentRuleType.All };
        _db.SegmentRules.Add(rule);
        await _db.SaveChangesAsync();

        var loaded = await _db.Segments
            .Include(s => s.Rules)
            .FirstAsync(s => s.Id == segment.Id);

        Assert.That(loaded.Rules, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenAnIdentityIsSaved_ThenItsTraitsCanBeLoaded()
    {
        var identity = new Identity { Identifier = "user-123", EnvironmentId = 1, CreatedAt = DateTimeOffset.UtcNow };
        _db.Identities.Add(identity);
        await _db.SaveChangesAsync();

        _db.IdentityTraits.Add(new IdentityTrait { IdentityId = identity.Id, Key = "plan", Value = "premium" });
        await _db.SaveChangesAsync();

        var loaded = await _db.Identities
            .Include(i => i.Traits)
            .FirstAsync(i => i.Id == identity.Id);

        Assert.That(loaded.Traits, Has.Count.EqualTo(1));
        Assert.That(loaded.Traits.First().Key, Is.EqualTo("plan"));
    }

    [Test]
    public async Task WhenAnAuditLogIsSaved_ThenItCanBeFilteredByOrganization()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ResourceType = "Feature",
            ResourceId = "1",
            Action = "Created",
            OrganizationId = 42,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        var logs = await _db.AuditLogs.Where(a => a.OrganizationId == 42).ToListAsync();

        Assert.That(logs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenAWebhookIsSaved_ThenItCanBeFilteredByEnvironment()
    {
        _db.Webhooks.Add(new Webhook
        {
            Url = "https://example.com/hook",
            Scope = WebhookScope.Environment,
            EnvironmentId = 5,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        var webhooks = await _db.Webhooks.Where(w => w.EnvironmentId == 5).ToListAsync();

        Assert.That(webhooks, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenAnApiKeyIsSaved_ThenItCanBeRetrievedByHashedKey()
    {
        _db.ApiKeys.Add(new ApiKey
        {
            Key = "hashed-value-xyz",
            Prefix = "hashed-va",
            Name = "CI Key",
            OrganizationId = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Key == "hashed-value-xyz");

        Assert.That(key, Is.Not.Null);
        Assert.That(key!.IsActive, Is.True);
    }

    [Test]
    public async Task WhenAUserIsSaved_ThenItCanBeRetrievedByEmail()
    {
        _db.Users.Add(new User
        {
            Email = "alice@example.com",
            PasswordHash = "hashed",
            FirstName = "Alice",
            LastName = "Smith",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "alice@example.com");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.FirstName, Is.EqualTo("Alice"));
    }
}
