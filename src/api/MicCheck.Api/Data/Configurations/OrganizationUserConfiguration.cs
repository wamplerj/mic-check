using MicCheck.Api.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class OrganizationUserConfiguration : IEntityTypeConfiguration<OrganizationUser>
{
    public void Configure(EntityTypeBuilder<OrganizationUser> builder)
    {
        builder.HasKey(ou => new { ou.OrganizationId, ou.UserId });
        builder.Property(ou => ou.Role).IsRequired();
        builder.Property(ou => ou.IsPrimary).IsRequired().HasDefaultValue(false);
    }
}
