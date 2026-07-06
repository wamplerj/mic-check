using MicCheck.Api.Common.Security.ApiKeys;
using MicCheck.Api.Audit;
using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Features;
using MicCheck.Api.Features.Usage;
using MicCheck.Api.Identities;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Segments;
using MicCheck.Api.Users;
using MicCheck.Api.Webhooks;
using Microsoft.EntityFrameworkCore;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Data;

public class MicCheckDbContext : DbContext, IMicCheckDbContext
{
    public MicCheckDbContext(DbContextOptions<MicCheckDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AppEnvironment> Environments => Set<AppEnvironment>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<FeatureState> FeatureStates => Set<FeatureState>();
    public DbSet<FeatureSegment> FeatureSegments => Set<FeatureSegment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Segment> Segments => Set<Segment>();
    public DbSet<SegmentRule> SegmentRules => Set<SegmentRule>();
    public DbSet<SegmentCondition> SegmentConditions => Set<SegmentCondition>();
    public DbSet<Identity> Identities => Set<Identity>();
    public DbSet<IdentityTrait> IdentityTraits => Set<IdentityTrait>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserProjectPermission> UserProjectPermissions => Set<UserProjectPermission>();
    public DbSet<FeatureUsageDaily> FeatureUsageDaily => Set<FeatureUsageDaily>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MicCheckDbContext).Assembly);
}

public interface IMicCheckDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationUser> OrganizationUsers { get; }
    DbSet<Project> Projects { get; }
    DbSet<AppEnvironment> Environments { get; }
    DbSet<Feature> Features { get; }
    DbSet<FeatureState> FeatureStates { get; }
    DbSet<FeatureSegment> FeatureSegments { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Segment> Segments { get; }
    DbSet<SegmentRule> SegmentRules { get; }
    DbSet<SegmentCondition> SegmentConditions { get; }
    DbSet<Identity> Identities { get; }
    DbSet<IdentityTrait> IdentityTraits { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Webhook> Webhooks { get; }
    DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserProjectPermission> UserProjectPermissions { get; }
    DbSet<FeatureUsageDaily> FeatureUsageDaily { get; }

    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
