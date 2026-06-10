using MicCheck.Api.Tests.Integration.Common;

namespace MicCheck.Api.Tests.Integration.Flags;

public sealed record FlagSeed(int OrganizationId, int ProjectId, int EnvironmentId, int FeatureId, int FeatureStateId, string EnvironmentApiKey, string FeatureName)
{
    public static async Task<FlagSeed> InsertEnabledFlagAsync(TestDatabase db, string testRunTag, bool enabled, string? value)
    {
        var unique = $"{testRunTag}-{Guid.NewGuid():N}"[..40];
        var environmentApiKey = $"envkey_{Guid.NewGuid():N}";
        var featureName = $"feature_{unique}";
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
            ("defaultEnabled", enabled),
            ("projectId", projectId),
            ("createdAt", now));

        var featureStateId = await db.ExecuteScalarAsync<int>(
            """
            INSERT INTO "FeatureStates" ("FeatureId", "EnvironmentId", "Enabled", "Value", "CreatedAt", "UpdatedAt", "Version")
            VALUES (@featureId, @environmentId, @enabled, @value, @createdAt, @updatedAt, 1)
            RETURNING "Id"
            """,
            ("featureId", featureId),
            ("environmentId", environmentId),
            ("enabled", enabled),
            ("value", (object?)value ?? DBNull.Value),
            ("createdAt", now),
            ("updatedAt", now));

        return new FlagSeed(organizationId, projectId, environmentId, featureId, featureStateId, environmentApiKey, featureName);
    }

    public async Task CleanupAsync(TestDatabase db)
        => await db.ExecuteAsync(
            "DELETE FROM \"Organizations\" WHERE \"Id\" = @orgId",
            ("orgId", OrganizationId));

    public async Task<string> DescribeAsync(TestDatabase db)
        => await db.SnapshotRowsAsync(
            """
            SELECT f."Id" AS "FeatureId", f."Name", f."DefaultEnabled",
                   fs."Id" AS "FeatureStateId", fs."Enabled", fs."Value", fs."EnvironmentId"
              FROM "Features" f
              JOIN "FeatureStates" fs ON fs."FeatureId" = f."Id"
             WHERE f."Id" = @featureId
            """,
            ("featureId", FeatureId));
}
