using MicCheck.Api.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.InviteToken).HasMaxLength(64);
        builder.HasIndex(o => o.InviteToken).IsUnique().HasFilter("\"InviteToken\" IS NOT NULL");

        builder.HasMany(o => o.Members)
            .WithOne()
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.ApiKeys)
            .WithOne()
            .HasForeignKey(k => k.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Projects)
            .WithOne()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
