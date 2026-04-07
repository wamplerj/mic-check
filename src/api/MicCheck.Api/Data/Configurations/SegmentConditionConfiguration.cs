using MicCheck.Api.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class SegmentConditionConfiguration : IEntityTypeConfiguration<SegmentCondition>
{
    public void Configure(EntityTypeBuilder<SegmentCondition> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Property).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Value).HasMaxLength(1_000).IsRequired();
    }
}
