using MicCheck.Api.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ResourceType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ResourceId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.OrganizationId, a.CreatedAt });
    }
}
