using MicCheck.Api.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class FeatureSegmentConfiguration : IEntityTypeConfiguration<FeatureSegment>
{
    public void Configure(EntityTypeBuilder<FeatureSegment> builder)
    {
        builder.HasKey(fs => fs.Id);

        builder.HasOne(fsg => fsg.FeatureState)
            .WithOne()
            .HasForeignKey<MicCheck.Api.Features.FeatureState>(fs => fs.FeatureSegmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
