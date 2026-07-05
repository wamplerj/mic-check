using MicCheck.Api.Tests.Integration.Common;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Integration.Flags;

[TestFixture]
public class FlagsApiAuthorizationTests
{
    private IntegrationTestSettings settings = null!;
    private FlagApiHttpClient client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        settings = IntegrationTestSettings.Load();
        client = new FlagApiHttpClient(settings);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => client.Dispose();

    [Test]
    public async Task WhenNoEnvironmentKeyIsProvided_ThenTheFlagsApiReturnsUnauthorized()
    {
        using var response = await client.SendAsync(
            httpFile: "Flags.http",
            requestName: "GetFlags",
            variables: new Dictionary<string, string>
            {
                ["baseUrl"] = settings.BaseUrl,
                ["environmentKey"] = string.Empty
            });

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(401),
            $"Expected 401 for a missing environment key, got {(int)response.StatusCode} {response.ReasonPhrase}.");
    }

    [Test]
    public async Task WhenAnUnknownEnvironmentKeyIsProvided_ThenTheFlagsApiReturnsUnauthorized()
    {
        using var response = await client.SendAsync(
            httpFile: "Flags.http",
            requestName: "GetFlags",
            variables: new Dictionary<string, string>
            {
                ["baseUrl"] = settings.BaseUrl,
                ["environmentKey"] = $"unknown_{Guid.NewGuid():N}"
            });

        Assert.That(
            (int)response.StatusCode,
            Is.EqualTo(401),
            $"Expected 401 for an unrecognized environment key, got {(int)response.StatusCode} {response.ReasonPhrase}.");
    }
}
