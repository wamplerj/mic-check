using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Url).HasMaxLength(500).IsRequired();
        builder.Property(w => w.Secret).HasMaxLength(200);
        builder.Property(w => w.CreatedAt).IsRequired();

        builder.HasIndex(w => w.EnvironmentId);
    }
}
