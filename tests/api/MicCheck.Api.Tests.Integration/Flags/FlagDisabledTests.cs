using System.Text.Json;
using MicCheck.Api.Tests.Integration.Common;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Integration.Flags;

[TestFixture]
public class FlagDisabledTests
{
    private IntegrationTestSettings settings = null!;
    private TestDatabase db = null!;
    private FlagApiHttpClient client = null!;
    private FlagSeed? seed;

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
        => seed = await FlagSeed.InsertEnabledFlagAsync(db, testRunTag: "FlagDisabled", enabled: false, value: null);

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
    public async Task WhenAFeatureIsDisabledInEnvironment_ThenTheFlagApiReportsItDisabled()
    {
        var current = seed!;

        using var response = await client.SendAsync(
            httpFile: "Flags.http",
            requestName: "GetFlags",
            variables: new Dictionary<string, string>
            {
                ["baseUrl"] = settings.BaseUrl,
                ["environmentKey"] = current.EnvironmentApiKey
            });

        var body = await response.Content.ReadAsStringAsync();

        Assert.That(
            response.IsSuccessStatusCode,
            Is.True,
            $"GET /api/v1/flags returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}. DB state:\n{await current.DescribeAsync(db)}");

        var flags = JsonSerializer.Deserialize<List<FlagResponseDto>>(body, JsonOptions)
            ?? throw new InvalidOperationException("Response body was not a JSON array.");

        var match = flags.SingleOrDefault(f => f.Feature.Name == current.FeatureName);

        Assert.That(
            match,
            Is.Not.Null,
            $"Feature '{current.FeatureName}' was not present in the flags response. Returned features: [{string.Join(", ", flags.Select(f => f.Feature.Name))}]. DB state:\n{await current.DescribeAsync(db)}");

        Assert.That(
            match!.Enabled,
            Is.False,
            $"Feature '{current.FeatureName}' was reported as enabled, but the seeded FeatureState has Enabled=false. DB state:\n{await current.DescribeAsync(db)}");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record FlagResponseDto(int Id, FeatureSummaryDto Feature, bool Enabled, string? FeatureStateValue);
    private sealed record FeatureSummaryDto(int Id, string Name);
}
