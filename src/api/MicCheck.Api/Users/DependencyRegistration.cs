using Microsoft.AspNetCore.Identity;

namespace MicCheck.Api.Users;

public static class DependencyRegistration
{
    public static IServiceCollection AddUsersServices(this IServiceCollection services)
    {
        services.AddScoped<UserService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }
}
