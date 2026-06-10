using MicCheck.Api.Common.Security.ApiKeys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Key).HasMaxLength(512).IsRequired();
        builder.Property(k => k.Prefix).HasMaxLength(8).IsRequired();
        builder.Property(k => k.Name).HasMaxLength(200).IsRequired();
        builder.Property(k => k.CreatedAt).IsRequired();

        builder.HasIndex(k => k.Key).IsUnique();
    }
}
