using MicCheck.Api.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicCheck.Api.Data.Configurations;

public class UserProjectPermissionConfiguration : IEntityTypeConfiguration<UserProjectPermission>
{
    public void Configure(EntityTypeBuilder<UserProjectPermission> builder)
    {
        builder.HasKey(p => new { p.UserId, p.ProjectId });

        builder.Property(p => p.Permissions)
            .HasConversion(
                v => string.Join(',', v.Select(p => p.ToString())),
                v => string.IsNullOrEmpty(v)
                    ? new List<ProjectPermission>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(Enum.Parse<ProjectPermission>)
                       .ToList());
    }
}
