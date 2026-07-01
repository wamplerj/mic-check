using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Data;

public static class DatabaseStartupExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MicCheckDbContext>();

        if (db.Database.ProviderName is null or "Microsoft.EntityFrameworkCore.InMemory")
            return;

        await db.Database.MigrateAsync(ct);
    }

    public static async Task SeedDevelopmentDataAsync(this WebApplication app, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(ct);
    }
}
