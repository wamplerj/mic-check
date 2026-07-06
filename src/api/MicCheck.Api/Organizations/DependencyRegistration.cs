namespace MicCheck.Api.Organizations;

public static class DependencyRegistration
{
    public static IServiceCollection AddOrganizationsServices(this IServiceCollection services)
    {
        services.AddScoped<OrganizationService>();

        return services;
    }
}
