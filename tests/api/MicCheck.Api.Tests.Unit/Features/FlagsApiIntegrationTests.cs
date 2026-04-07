using System.Net;
using System.Net.Http.Json;
using MicCheck.Api.Data;
using MicCheck.Api.Features;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Features;

[TestFixture]
public class FlagsApiIntegrationTests
{
    private MicCheckWebApplicationFactory _factory = null!;
    private string _envApiKey = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new MicCheckWebApplicationFactory();
        _envApiKey = SeedEnvironmentWithFlag();
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    private string SeedEnvironmentWithFlag()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();

        var apiKey = $"test-env-key-{Guid.NewGuid():N}";

        var project = new MicCheck.Api.Projects.Project
        {
            Name = "Test Project",
            OrganizationId = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        db.SaveChanges();

        var environment = new AppEnvironment
        {
            Name = "Test Env",
            ApiKey = apiKey,
            ProjectId = project.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Environments.Add(environment);
        db.SaveChanges();

        var feature = new Feature
        {
            Name = "dark_mode",
            ProjectId = project.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Features.Add(feature);
        db.SaveChanges();

        db.FeatureStates.Add(new FeatureState
        {
            FeatureId = feature.Id,
            EnvironmentId = environment.Id,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();

        return apiKey;
    }

    [Test]
    public async Task WhenEnvironmentKeyIsValid_ThenFlagsAreReturned()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Environment-Key", _envApiKey);

        var response = await client.GetAsync("/api/v1/flags/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var flags = await response.Content.ReadFromJsonAsync<List<FlagResponse>>();
        Assert.That(flags, Has.Count.EqualTo(1));
        Assert.That(flags![0].Feature.Name, Is.EqualTo("dark_mode"));
        Assert.That(flags[0].Enabled, Is.True);
    }

    [Test]
    public async Task WhenEnvironmentKeyIsAbsent_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/flags/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenEnvironmentKeyIsInvalid_ThenUnauthorizedIsReturned()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Environment-Key", "not-a-real-key");

        var response = await client.GetAsync("/api/v1/flags/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhenIdentifyingUserWithValidKey_ThenFlagsAndTraitsAreReturned()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Environment-Key", _envApiKey);

        var response = await client.PostAsJsonAsync("/api/v1/identities/", new
        {
            identifier = "user-123",
            traits = new[] { new { trait_key = "plan", trait_value = "premium" } }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task WhenGettingEnvironmentDocument_ThenDocumentIsReturned()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Environment-Key", _envApiKey);

        var response = await client.GetAsync("/api/v1/environment-document/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
