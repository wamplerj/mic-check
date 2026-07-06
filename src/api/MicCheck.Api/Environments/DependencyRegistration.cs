namespace MicCheck.Api.Environments;

public static class DependencyRegistration
{
    public static IServiceCollection AddEnvironmentsServices(this IServiceCollection services)
    {
        services.AddScoped<EnvironmentService>();
        services.AddScoped<EnvironmentDocumentService>();

        return services;
    }
}
