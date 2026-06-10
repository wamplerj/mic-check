using MicCheck.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MicCheck.Api.Tests.Unit;

public class MicCheckWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryEfServiceProvider =
        new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbName = Guid.NewGuid().ToString();

        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MicCheckDbContext));
            if (contextDescriptor is not null)
                services.Remove(contextDescriptor);

            var optionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MicCheckDbContext>));
            if (optionsDescriptor is not null)
                services.Remove(optionsDescriptor);

            services.AddDbContext<MicCheckDbContext>(options =>
                options
                    .UseInMemoryDatabase(dbName)
                    .UseInternalServiceProvider(InMemoryEfServiceProvider));
        });
    }
}
