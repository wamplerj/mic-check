namespace MicCheck.Api.Projects;

public static class DependencyRegistration
{
    public static IServiceCollection AddProjectsServices(this IServiceCollection services)
    {
        services.AddScoped<ProjectService>();

        return services;
    }
}
