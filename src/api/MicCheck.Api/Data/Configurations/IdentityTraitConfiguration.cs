using MicCheck.Api.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class IdentityTraitConfiguration : IEntityTypeConfiguration<IdentityTrait>
{
    public void Configure(EntityTypeBuilder<IdentityTrait> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Key).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Value).HasMaxLength(2_000).IsRequired();

        builder.HasIndex(t => new { t.IdentityId, t.Key }).IsUnique();
    }
}
