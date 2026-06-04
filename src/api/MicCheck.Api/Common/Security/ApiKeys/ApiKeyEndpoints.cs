using MicCheck.Api.Common.Security.Authorization;

namespace MicCheck.Api.Common.Security.ApiKeys;

public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/organisations/{organizationId:int}/api-keys")
            .RequireAuthorization(AuthorizationPolicies.OrganizationAdmin)
            .WithTags("ApiKeys");

        group.MapPost("/", async (
            int organizationId,
            CreateApiKeyRequest request,
            ApiKeyService apiKeyService,
            CancellationToken ct) =>
        {
            var (key, rawKey) = await apiKeyService.CreateAsync(
                organizationId, request.Name, request.ExpiresAt, ct);

            return Results.Ok(new CreateApiKeyResponse(key.Id, key.Name, rawKey, key.Prefix, key.ExpiresAt));
        }).WithName("CreateApiKey");

        group.MapGet("/", async (
            int organizationId,
            ApiKeyService apiKeyService,
            CancellationToken ct) =>
        {
            var keys = await apiKeyService.ListAsync(organizationId, ct);
            return Results.Ok(keys.Select(k =>
                new ApiKeyResponse(k.Id, k.Name, k.Prefix, k.IsActive, k.ExpiresAt, k.CreatedAt)));
        }).WithName("ListApiKeys");

        group.MapDelete("/{keyId:int}", async (
            int organizationId,
            int keyId,
            ApiKeyService apiKeyService,
            CancellationToken ct) =>
        {
            await apiKeyService.RevokeAsync(organizationId, keyId, ct);
            return Results.NoContent();
        }).WithName("RevokeApiKey");
    }
}
