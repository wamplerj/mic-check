using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class FeatureStateConfiguration : IEntityTypeConfiguration<FeatureState>
{
    public void Configure(EntityTypeBuilder<FeatureState> builder)
    {
        builder.HasKey(fs => fs.Id);
        builder.Property(fs => fs.Value).HasMaxLength(20_000);
        builder.Property(fs => fs.CreatedAt).IsRequired();
        builder.Property(fs => fs.UpdatedAt).IsRequired();
        builder.Property(fs => fs.Version).IsConcurrencyToken();

        builder.HasIndex(fs => new { fs.FeatureId, fs.EnvironmentId, fs.IdentityId }).IsUnique();
        builder.HasIndex(fs => fs.EnvironmentId);
        builder.HasIndex(fs => fs.IdentityId);
    }
}
