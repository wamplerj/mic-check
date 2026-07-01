using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Environments;

public record EnvironmentResponse(
    int Id,
    string Name,
    string ApiKey,
    int ProjectId,
    DateTimeOffset CreatedAt
)
{
    public static EnvironmentResponse From(AppEnvironment env) => new(
        env.Id, env.Name, env.ApiKey, env.ProjectId, env.CreatedAt);
}
