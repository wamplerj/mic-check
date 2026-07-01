using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).HasMaxLength(150).IsRequired();
        builder.Property(f => f.InitialValue).HasMaxLength(20_000);
        builder.Property(f => f.Description).HasMaxLength(2_000);
        builder.Property(f => f.CreatedAt).IsRequired();

        builder.HasIndex(f => new { f.ProjectId, f.Name }).IsUnique();

        builder.HasMany(f => f.FeatureStates)
            .WithOne()
            .HasForeignKey(fs => fs.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Tags)
            .WithMany()
            .UsingEntity("FeatureTags");
    }
}
