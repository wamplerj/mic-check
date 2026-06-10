using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.EventType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.AttemptedAt).IsRequired();

        builder.HasIndex(d => d.WebhookId);
    }
}
