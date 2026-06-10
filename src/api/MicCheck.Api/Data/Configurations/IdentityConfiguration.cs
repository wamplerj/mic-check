using MicCheck.Api.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class IdentityConfiguration : IEntityTypeConfiguration<Identity>
{
    public void Configure(EntityTypeBuilder<Identity> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Identifier).HasMaxLength(2_000).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasIndex(i => new { i.EnvironmentId, i.Identifier }).IsUnique();

        builder.HasMany(i => i.Traits)
            .WithOne()
            .HasForeignKey(t => t.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.FeatureStateOverrides)
            .WithOne()
            .HasForeignKey(fs => fs.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
