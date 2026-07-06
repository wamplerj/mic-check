namespace MicCheck.Api.Identities;

public static class DependencyRegistration
{
    public static IServiceCollection AddIdentitiesServices(this IServiceCollection services)
    {
        services.AddScoped<IdentityResolutionService>();
        services.AddScoped<AdminIdentityService>();

        return services;
    }
}
