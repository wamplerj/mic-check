using System.Net;
using System.Net.Http.Json;
using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Admin;

[TestFixture]
public class AdminApiIntegrationTests
{
    private MicCheckWebApplicationFactory _factory = null!;
    private string _rawApiKey = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MicCheckWebApplicationFactory();
        _rawApiKey = SeedOrganizationWithApiKey();
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    private string SeedOrganizationWithApiKey()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();

        var org = new Organization { Name = "Test Org", CreatedAt = DateTimeOffset.UtcNow };
        db.Organizations.Add(org);
        db.SaveChanges();

        var rawKey = ApiKeyHasher.GenerateKey();
        var hashedKey = ApiKeyHasher.Hash(rawKey);
        var prefix = rawKey.Length >= 8 ? rawKey[..8] : rawKey;

        db.ApiKeys.Add(new ApiKey
        {
            Key = hashedKey,
            Prefix = prefix,
            Name = "Test Key",
            OrganizationId = org.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        return rawKey;
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Api-Key {_rawApiKey}");
        return client;
    }

    [Test]
    public async Task WhenCreatingAProject_ThenProjectIsReturned()
    {
        var client = CreateAuthenticatedClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var org = db.Organizations.First();

        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            name = "My Project",
            organizationId = org.Id
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task WhenCreatingAFeature_ThenFeatureIsReturned()
    {
        var client = CreateAuthenticatedClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var org = db.Organizations.First();

        var project = new Project
        {
            Name = "My Project",
            OrganizationId = org.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        db.SaveChanges();

        var response = await client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/features", new
        {
            name = "dark_mode",
            type = "Standard",
            initialValue = (string?)null,
            description = (string?)null
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task WhenCreatingAFeature_ThenFeatureStateIsAutoCreatedForEnvironments()
    {
        var client = CreateAuthenticatedClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var org = db.Organizations.First();

        var project = new Project
        {
            Name = "My Project",
            OrganizationId = org.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        db.SaveChanges();

        db.Environments.Add(new AppEnvironment
        {
            Name = "Production",
            ApiKey = "env-key-prod",
            ProjectId = project.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        await client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/features", new
        {
            name = "flag_x",
            type = "Standard",
            initialValue = (string?)null,
            description = (string?)null
        });

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var feature = verifyDb.Features.First(f => f.Name == "flag_x");
        var states = verifyDb.FeatureStates.Where(fs => fs.FeatureId == feature.Id).ToList();

        Assert.That(states, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenCreatingAnEnvironment_ThenFeatureStateIsAutoCreatedForExistingFeatures()
    {
        var client = CreateAuthenticatedClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var org = db.Organizations.First();

        var project = new Project
        {
            Name = "My Project",
            OrganizationId = org.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        db.Features.Add(new Feature
        {
            Name = "existing_flag",
            ProjectId = project.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        var response = await client.PostAsJsonAsync("/api/v1/environments", new
        {
            name = "Staging",
            projectId = project.Id
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var feature = verifyDb.Features.First(f => f.Name == "existing_flag");
        var env = verifyDb.Environments.First(e => e.ProjectId == project.Id);
        var states = verifyDb.FeatureStates.Where(fs => fs.EnvironmentId == env.Id && fs.FeatureId == feature.Id).ToList();

        Assert.That(states, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WhenRequestIsMissingApiKey_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/projects?organizationId=1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenCreatingAFeatureWithInvalidName_ThenUnprocessableEntityIsReturned()
    {
        var client = CreateAuthenticatedClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var org = db.Organizations.First();

        var project = new Project
        {
            Name = "My Project",
            OrganizationId = org.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        db.SaveChanges();

        var response = await client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/features", new
        {
            name = "invalid name with spaces!",
            type = "Standard"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }

    [Test]
    public async Task WhenCreatingASegment_ThenSegmentIsReturned()
    {
        var client = CreateAuthenticatedClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();
        var org = db.Organizations.First();

        var project = new Project
        {
            Name = "My Project",
            OrganizationId = org.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        db.SaveChanges();

        var response = await client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/segments", new
        {
            name = "Premium Users",
            rules = new[]
            {
                new
                {
                    type = "All",
                    conditions = new[]
                    {
                        new { property = "plan", @operator = "Equal", value = "premium" }
                    }
                }
            }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
}
