using MicCheck.Api.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(500).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.Organizations)
            .WithOne()
            .HasForeignKey(ou => ou.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
