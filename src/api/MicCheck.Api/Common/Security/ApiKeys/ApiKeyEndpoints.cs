using System.Diagnostics.CodeAnalysis;
using MicCheck.Api.Common.Security.Authorization;

namespace MicCheck.Api.Common.Security.ApiKeys;

[ExcludeFromCodeCoverage(Justification = "Minimal-API route registration; requires a live HTTP pipeline to exercise, which CLAUDE.md disallows (no WebApplicationFactory/InMemory). Branch logic is covered via ApiKeyService unit tests.")]
public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/organisation/{organizationId:int}")
            .RequireAuthorization(AuthorizationPolicies.OrganizationAdmin)
            .WithTags("ApiKeys");

        group.MapPost("/api-keys", async (int organizationId, CreateApiKeyRequest request, ApiKeyService apiKeyService, CancellationToken ct) =>
        {
            var result = await apiKeyService.CreateAsync(organizationId, request.Name, request.ExpiresAt, ct);

            return Results.Ok(new CreateApiKeyResponse(result.Key.Id, result.Key.Name, result.RawKey, result.Key.Prefix, result.Key.ExpiresAt));

        }).WithName("CreateApiKey");

        group.MapGet("/api-keys", async (int organizationId, ApiKeyService apiKeyService, CancellationToken ct) =>
        {
            var keys = await apiKeyService.ListAsync(organizationId, ct);
            return Results.Ok(keys.Select(k => new ApiKeyResponse(k.Id, k.Name, k.Prefix, k.IsActive, k.ExpiresAt, k.CreatedAt)));

        }).WithName("ListApiKeys");

        group.MapDelete("/api-key/{keyId:int}", async (int organizationId, int keyId, ApiKeyService apiKeyService, CancellationToken ct) =>
        {
            await apiKeyService.RevokeAsync(organizationId, keyId, ct);
            return Results.NoContent();

        }).WithName("RevokeApiKey");
    }
}
