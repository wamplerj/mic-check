using MicCheck.Api.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class SegmentRuleConfiguration : IEntityTypeConfiguration<SegmentRule>
{
    public void Configure(EntityTypeBuilder<SegmentRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasMany(r => r.Conditions)
            .WithOne()
            .HasForeignKey(c => c.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.ChildRules)
            .WithOne()
            .HasForeignKey(r => r.ParentRuleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
