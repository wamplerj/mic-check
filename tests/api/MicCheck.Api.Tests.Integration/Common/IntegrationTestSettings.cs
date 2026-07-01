using NUnit.Framework;

namespace MicCheck.Api.Tests.Integration.Common;

public sealed record IntegrationTestSettings(string BaseUrl, string DbConnectionString, bool AcceptAnyServerCertificate)
{
    public static IntegrationTestSettings Load()
    {
        var baseUrl = Resolve("BaseUrl", "MICCHECK_API_BASE_URL")
            ?? throw new InvalidOperationException(
                "BaseUrl is not set. Provide it via .runsettings (TestRunParameters.BaseUrl) or the MICCHECK_API_BASE_URL environment variable.");

        var connectionString = Resolve("DbConnectionString", "MICCHECK_DB_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "DbConnectionString is not set. Provide it via .runsettings (TestRunParameters.DbConnectionString) or the MICCHECK_DB_CONNECTION_STRING environment variable.");

        var acceptAnyCert = bool.TryParse(
            Resolve("AcceptAnyServerCertificate", "MICCHECK_ACCEPT_ANY_SERVER_CERTIFICATE"),
            out var parsed) && parsed;

        return new IntegrationTestSettings(baseUrl.TrimEnd('/'), connectionString, acceptAnyCert);
    }

    private static string? Resolve(string runSettingsParameter, string environmentVariable)
    {
        var fromEnv = System.Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var fromRunSettings = TestContext.Parameters.Get(runSettingsParameter);
        return string.IsNullOrWhiteSpace(fromRunSettings) ? null : fromRunSettings;
    }
}
