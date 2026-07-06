using MicCheck.Api.Features.Usage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class FeatureUsageDailyConfiguration : IEntityTypeConfiguration<FeatureUsageDaily>
{
    public void Configure(EntityTypeBuilder<FeatureUsageDaily> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.FeatureName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.Count).IsRequired();

        builder.HasIndex(u => new { u.EnvironmentId, u.FeatureId, u.UsageDate }).IsUnique();
        builder.HasIndex(u => new { u.EnvironmentId, u.UsageDate });
    }
}
