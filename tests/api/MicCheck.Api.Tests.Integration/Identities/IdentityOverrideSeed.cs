using MicCheck.Api.Tests.Integration.Common;

namespace MicCheck.Api.Tests.Integration.Identities;

/// <summary>
/// Seeds an environment-default FeatureState plus an identity-level override for the same
/// feature, so a test can assert that identity overrides take precedence over the environment
/// default when evaluating <c>/api/v1/identity</c>.
/// </summary>
public sealed record IdentityOverrideSeed(
    int OrganizationId,
    int EnvironmentId,
    int FeatureId,
    string EnvironmentApiKey,
    string FeatureName,
    string Identifier)
{
    public static async Task<IdentityOverrideSeed> InsertAsync(
        TestDatabase db,
        string testRunTag,
        bool environmentDefaultEnabled,
        bool identityOverrideEnabled)
    {
        var rawUnique = $"{testRunTag}-{Guid.NewGuid():N}";
        var unique = rawUnique[..Math.Min(rawUnique.Length, 40)];
        var environmentApiKey = $"envkey_{Guid.NewGuid():N}";
        var featureName = $"feature_{unique}";
        var identifier = $"identity_{unique}";
        var now = DateTimeOffset.UtcNow;

        var organizationId = await db.ExecuteScalarAsync<int>(
            "INSERT INTO \"Organizations\" (\"Name\", \"CreatedAt\") VALUES (@name, @createdAt) RETURNING \"Id\"",
            ("name", $"org_{unique}"),
            ("createdAt", now));

        var projectId = await db.ExecuteScalarAsync<int>(
            "INSERT INTO \"Projects\" (\"Name\", \"OrganizationId\", \"CreatedAt\", \"HideDisabledFlags\") VALUES (@name, @orgId, @createdAt, false) RETURNING \"Id\"",
            ("name", $"project_{unique}"),
            ("orgId", organizationId),
            ("createdAt", now));

        var environmentId = await db.ExecuteScalarAsync<int>(
            "INSERT INTO \"Environments\" (\"Name\", \"ApiKey\", \"ProjectId\", \"CreatedAt\") VALUES (@name, @apiKey, @projectId, @createdAt) RETURNING \"Id\"",
            ("name", $"env_{unique}"),
            ("apiKey", environmentApiKey),
            ("projectId", projectId),
            ("createdAt", now));

        var featureId = await db.ExecuteScalarAsync<int>(
            "INSERT INTO \"Features\" (\"Name\", \"Type\", \"DefaultEnabled\", \"ProjectId\", \"CreatedAt\") VALUES (@name, 0, @defaultEnabled, @projectId, @createdAt) RETURNING \"Id\"",
            ("name", featureName),
            ("defaultEnabled", environmentDefaultEnabled),
            ("projectId", projectId),
            ("createdAt", now));

        await db.ExecuteScalarAsync<int>(
            """
            INSERT INTO "FeatureStates" ("FeatureId", "EnvironmentId", "Enabled", "Value", "CreatedAt", "UpdatedAt", "Version")
            VALUES (@featureId, @environmentId, @enabled, NULL, @createdAt, @updatedAt, 1)
            RETURNING "Id"
            """,
            ("featureId", featureId),
            ("environmentId", environmentId),
            ("enabled", environmentDefaultEnabled),
            ("createdAt", now),
            ("updatedAt", now));

        var identityId = await db.ExecuteScalarAsync<int>(
            "INSERT INTO \"Identities\" (\"Identifier\", \"EnvironmentId\", \"CreatedAt\") VALUES (@identifier, @environmentId, @createdAt) RETURNING \"Id\"",
            ("identifier", identifier),
            ("environmentId", environmentId),
            ("createdAt", now));

        await db.ExecuteScalarAsync<int>(
            """
            INSERT INTO "FeatureStates" ("FeatureId", "EnvironmentId", "IdentityId", "Enabled", "Value", "CreatedAt", "UpdatedAt", "Version")
            VALUES (@featureId, @environmentId, @identityId, @enabled, NULL, @createdAt, @updatedAt, 1)
            RETURNING "Id"
            """,
            ("featureId", featureId),
            ("environmentId", environmentId),
            ("identityId", identityId),
            ("enabled", identityOverrideEnabled),
            ("createdAt", now),
            ("updatedAt", now));

        return new IdentityOverrideSeed(organizationId, environmentId, featureId, environmentApiKey, featureName, identifier);
    }

    public async Task CleanupAsync(TestDatabase db)
        => await db.ExecuteAsync(
            "DELETE FROM \"Organizations\" WHERE \"Id\" = @orgId",
            ("orgId", OrganizationId));

    public async Task<string> DescribeAsync(TestDatabase db)
        => await db.SnapshotRowsAsync(
            """
            SELECT fs."Id" AS "FeatureStateId", fs."Enabled", fs."IdentityId", fs."FeatureSegmentId"
              FROM "FeatureStates" fs
             WHERE fs."FeatureId" = @featureId
            """,
            ("featureId", FeatureId));
}
