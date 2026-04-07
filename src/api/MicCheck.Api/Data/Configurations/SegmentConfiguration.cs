using MicCheck.Api.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class SegmentConfiguration : IEntityTypeConfiguration<Segment>
{
    public void Configure(EntityTypeBuilder<Segment> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasMany(s => s.Rules)
            .WithOne()
            .HasForeignKey(r => r.SegmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
