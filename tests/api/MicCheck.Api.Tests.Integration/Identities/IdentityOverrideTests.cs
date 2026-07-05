using System.Text.Json;
using MicCheck.Api.Tests.Integration.Common;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Integration.Identities;

[TestFixture]
public class IdentityOverrideTests
{
    private IntegrationTestSettings settings = null!;
    private TestDatabase db = null!;
    private FlagApiHttpClient client = null!;
    private IdentityOverrideSeed? seed;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        settings = IntegrationTestSettings.Load();
        db = new TestDatabase(settings.DbConnectionString);
        client = new FlagApiHttpClient(settings);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => client.Dispose();

    [SetUp]
    public async Task SetUp()
        => seed = await IdentityOverrideSeed.InsertAsync(
            db,
            testRunTag: "IdentityOverride",
            environmentDefaultEnabled: false,
            identityOverrideEnabled: true);

    [TearDown]
    public async Task TearDown()
    {
        if (seed is not null)
        {
            await seed.CleanupAsync(db);
            seed = null;
        }
    }

    [Test]
    public async Task WhenAnIdentityHasAFeatureOverride_ThenItTakesPrecedenceOverTheEnvironmentDefault()
    {
        var current = seed!;

        using var response = await client.SendAsync(
            httpFile: "Identity.http",
            requestName: "GetIdentity",
            variables: new Dictionary<string, string>
            {
                ["baseUrl"] = settings.BaseUrl,
                ["environmentKey"] = current.EnvironmentApiKey,
                ["identifier"] = current.Identifier
            });

        var body = await response.Content.ReadAsStringAsync();

        Assert.That(
            response.IsSuccessStatusCode,
            Is.True,
            $"GET /api/v1/identity returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}. DB state:\n{await current.DescribeAsync(db)}");

        var identity = JsonSerializer.Deserialize<IdentityResponseDto>(body, JsonOptions)
            ?? throw new InvalidOperationException("Response body was not the expected identity shape.");

        var match = identity.Flags.SingleOrDefault(f => f.Feature.Name == current.FeatureName);

        Assert.That(
            match,
            Is.Not.Null,
            $"Feature '{current.FeatureName}' was not present in the identity response. DB state:\n{await current.DescribeAsync(db)}");

        Assert.That(
            match!.Enabled,
            Is.True,
            $"Expected the identity override (Enabled=true) to win over the environment default (Enabled=false). DB state:\n{await current.DescribeAsync(db)}");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record IdentityResponseDto(List<object> Traits, List<FlagResponseDto> Flags);
    private sealed record FlagResponseDto(int Id, FeatureSummaryDto Feature, bool Enabled, string? FeatureStateValue);
    private sealed record FeatureSummaryDto(int Id, string Name);
}
