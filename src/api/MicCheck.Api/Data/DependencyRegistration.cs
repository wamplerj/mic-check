using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Data;

public static class DependencyRegistration
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<DatabaseSeeder>();

        var connectionString = configuration.GetConnectionString("miccheck")
            ?? (System.Environment.GetEnvironmentVariable("DATABASE_URL") is { } databaseUrl
                ? DatabaseUrlParser.ToNpgsqlConnectionString(databaseUrl)
                : configuration.GetConnectionString("DefaultConnection")!);

        services.AddDbContext<MicCheckDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMicCheckDbContext>(sp => sp.GetRequiredService<MicCheckDbContext>());

        return services;
    }
}
