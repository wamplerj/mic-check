using System.Text.Json;
using MicCheck.Api.Tests.Integration.Common;
using MicCheck.Api.Tests.Integration.Flags;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Integration.Environments;

[TestFixture]
public class EnvironmentDocumentTests
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
        => seed = await FlagSeed.InsertEnabledFlagAsync(db, testRunTag: "EnvDocument", enabled: true, value: null);

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
    public async Task WhenAnEnvironmentHasFeatureStates_ThenTheEnvironmentDocumentIncludesThem()
    {
        var current = seed!;

        using var response = await client.SendAsync(
            httpFile: "EnvironmentDocument.http",
            requestName: "GetEnvironmentDocument",
            variables: new Dictionary<string, string>
            {
                ["baseUrl"] = settings.BaseUrl,
                ["environmentKey"] = current.EnvironmentApiKey
            });

        var body = await response.Content.ReadAsStringAsync();

        Assert.That(
            response.IsSuccessStatusCode,
            Is.True,
            $"GET /api/v1/environment-document returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}. DB state:\n{await current.DescribeAsync(db)}");

        var document = JsonSerializer.Deserialize<EnvironmentDocumentDto>(body, JsonOptions)
            ?? throw new InvalidOperationException("Response body was not the expected environment document shape.");

        Assert.That(
            document.Id,
            Is.EqualTo(current.EnvironmentId),
            $"Environment document Id did not match the seeded environment. DB state:\n{await current.DescribeAsync(db)}");

        var match = document.FeatureStates.SingleOrDefault(f => f.Feature.Name == current.FeatureName);

        Assert.That(
            match,
            Is.Not.Null,
            $"Feature '{current.FeatureName}' was not present in the environment document. Returned features: [{string.Join(", ", document.FeatureStates.Select(f => f.Feature.Name))}]. DB state:\n{await current.DescribeAsync(db)}");

        Assert.That(match!.Enabled, Is.True);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record EnvironmentDocumentDto(int Id, string ApiKey, List<FlagResponseDto> FeatureStates, EnvironmentProjectDto Project);
    private sealed record EnvironmentProjectDto(int Id, string Name, List<object> Segments);
    private sealed record FlagResponseDto(int Id, FeatureSummaryDto Feature, bool Enabled, string? FeatureStateValue);
    private sealed record FeatureSummaryDto(int Id, string Name);
}
