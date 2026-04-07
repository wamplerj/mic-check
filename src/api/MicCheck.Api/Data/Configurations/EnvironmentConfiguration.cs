using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Data.Configurations;

public class EnvironmentConfiguration : IEntityTypeConfiguration<AppEnvironment>
{
    public void Configure(EntityTypeBuilder<AppEnvironment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ApiKey).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.ApiKey).IsUnique();
    }
}
