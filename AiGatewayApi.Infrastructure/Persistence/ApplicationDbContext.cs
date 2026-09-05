using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Domain.Common;
using AiGatewayApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<AiProvider> AiProviders => Set<AiProvider>();
    public DbSet<AiModel> AiModels => Set<AiModel>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();
    public DbSet<Quota> Quotas => Set<Quota>();
    public DbSet<QuotaUsage> QuotaUsages => Set<QuotaUsage>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<PromptTemplateVersion> PromptTemplateVersions => Set<PromptTemplateVersion>();
    public DbSet<RoutingPolicy> RoutingPolicies => Set<RoutingPolicy>();
    public DbSet<AsyncJob> AsyncJobs => Set<AsyncJob>();
    public DbSet<ContentPolicy> ContentPolicies => Set<ContentPolicy>();
    public DbSet<ProviderHealth> ProviderHealthChecks => Set<ProviderHealth>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IHasConcurrency).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IHasConcurrency.RowVersion))
                    .IsRowVersion();
            }
        }

        modelBuilder.Entity<AiProvider>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AiModel>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ApiKey>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Quota>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<QuotaUsage>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PromptTemplate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PromptTemplateVersion>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RoutingPolicy>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AsyncJob>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ContentPolicy>().HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<AiProvider>(b =>
        {
            b.ToTable("ai_providers");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(200).IsRequired();
            b.Property(e => e.ProviderType).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.HealthStatus).HasConversion<string>().HasMaxLength(30);
            b.Property(e => e.HeadersJson).HasColumnType("jsonb");
            b.Property(e => e.OrganizationId).HasMaxLength(100);
            b.Property(e => e.AzureDeployment).HasMaxLength(100);
            b.HasIndex(e => e.ProviderType);
        });

        modelBuilder.Entity<AiModel>(b =>
        {
            b.ToTable("ai_models");
            b.HasKey(e => e.Id);
            b.Property(e => e.ModelCode).HasMaxLength(120).IsRequired();
            b.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            b.Property(e => e.PriceUnit).HasConversion<string>().HasMaxLength(30);
            b.Property(e => e.InputPricePer1K).HasPrecision(18, 8);
            b.Property(e => e.OutputPricePer1K).HasPrecision(18, 8);
            b.Property(e => e.OutputPricePer1KCached).HasPrecision(18, 8);
            b.HasIndex(e => new { e.ProviderId, e.ModelCode }).IsUnique();
            b.HasOne(e => e.Provider)
                .WithMany(p => p.Models)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(b =>
        {
            b.ToTable("projects");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(200).IsRequired();
            b.Property(e => e.Code).HasMaxLength(50).IsRequired();
            b.Property(e => e.AllowedProviderTypes).HasColumnType("jsonb");
            b.HasIndex(e => e.Code).IsUnique();
            b.HasIndex(e => e.CompanyId);
            b.HasOne(e => e.DefaultModel)
                .WithMany()
                .HasForeignKey(e => e.DefaultModelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApiKey>(b =>
        {
            b.ToTable("api_keys");
            b.HasKey(e => e.Id);
            b.Property(e => e.KeyPrefix).HasMaxLength(64).IsRequired();
            b.Property(e => e.KeyHash).HasMaxLength(500).IsRequired();
            b.Property(e => e.Name).HasMaxLength(150).IsRequired();
            b.Property(e => e.AllowedModels).HasColumnType("jsonb");
            b.Property(e => e.Scopes).HasColumnType("jsonb");
            b.HasIndex(e => e.KeyPrefix);
            b.HasIndex(e => e.ProjectId);
            b.HasOne(e => e.Project)
                .WithMany(p => p.ApiKeys)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Quota>(b =>
        {
            b.ToTable("quotas");
            b.HasKey(e => e.Id);
            b.Property(e => e.CostLimitUsd).HasPrecision(18, 6);
            b.Property(e => e.CurrentMonthCostUsd).HasPrecision(18, 6);
            b.Property(e => e.AlertThreshold).HasPrecision(5, 2);
            b.HasIndex(e => e.ProjectId).IsUnique();
            b.HasOne(e => e.Project)
                .WithOne(p => p.Quota)
                .HasForeignKey<Quota>(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuotaUsage>(b =>
        {
            b.ToTable("quota_usages");
            b.HasKey(e => e.Id);
            b.Property(e => e.Period).HasConversion<string>().HasMaxLength(20);
            b.Property(e => e.CostUsd).HasPrecision(18, 6);
            b.HasIndex(e => new { e.ProjectId, e.Period, e.PeriodStart }).IsUnique();
            b.HasOne(e => e.Project)
                .WithMany(p => p.QuotaUsages)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UsageLog>(b =>
        {
            b.ToTable("usage_logs");
            b.HasKey(e => e.Id);
            b.Property(e => e.ProviderType).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.FallbackFromProvider).HasConversion<string>().HasMaxLength(50);
            b.Property(e => e.Endpoint).HasConversion<string>().HasMaxLength(30);
            b.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(e => e.CostUsd).HasPrecision(18, 8);
            b.Property(e => e.ErrorCode).HasMaxLength(80);
            b.Property(e => e.RequestId).HasMaxLength(64);
            b.HasIndex(e => e.CreatedAt);
            b.HasIndex(e => e.ProjectId);
            b.HasIndex(e => e.CompanyId);
            b.HasOne(e => e.Project)
                .WithMany(p => p.UsageLogs)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.ApiKey)
                .WithMany(k => k.UsageLogs)
                .HasForeignKey(e => e.ApiKeyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Model)
                .WithMany(m => m.UsageLogs)
                .HasForeignKey(e => e.ModelId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.PromptTemplate)
                .WithMany(t => t.UsageLogs)
                .HasForeignKey(e => e.PromptTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PromptTemplate>(b =>
        {
            b.ToTable("prompt_templates");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(80).IsRequired();
            b.Property(e => e.Name).HasMaxLength(200).IsRequired();
            b.Property(e => e.SourceSystem).HasMaxLength(50);
            b.Property(e => e.Module).HasMaxLength(80);
            b.Property(e => e.VariablesSchemaJson).HasColumnType("jsonb");
            b.HasIndex(e => e.Code).IsUnique();
            b.HasOne(e => e.Project)
                .WithMany(p => p.PromptTemplates)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.PublishedVersion)
                .WithMany()
                .HasForeignKey(e => e.PublishedVersionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PromptTemplateVersion>(b =>
        {
            b.ToTable("prompt_template_versions");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.TemplateId, e.VersionNumber }).IsUnique();
            b.HasOne(e => e.Template)
                .WithMany(t => t.Versions)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoutingPolicy>(b =>
        {
            b.ToTable("routing_policies");
            b.HasKey(e => e.Id);
            b.Property(e => e.ConditionJson).HasColumnType("jsonb");
            b.HasIndex(e => new { e.ProjectId, e.Priority });
            b.HasOne(e => e.Project)
                .WithMany(p => p.RoutingPolicies)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.Model)
                .WithMany(m => m.RoutingPolicies)
                .HasForeignKey(e => e.ModelId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(e => e.Provider)
                .WithMany(p => p.RoutingPolicies)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AsyncJob>(b =>
        {
            b.ToTable("async_jobs");
            b.HasKey(e => e.Id);
            b.Property(e => e.Endpoint).HasConversion<string>().HasMaxLength(30);
            b.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            b.HasIndex(e => e.Status);
            b.HasIndex(e => e.ProjectId);
            b.HasOne(e => e.Project)
                .WithMany(p => p.AsyncJobs)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(e => e.ApiKey)
                .WithMany()
                .HasForeignKey(e => e.ApiKeyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ContentPolicy>(b =>
        {
            b.ToTable("content_policies");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(150).IsRequired();
            b.Property(e => e.BlockedPatternsJson).HasColumnType("jsonb");
            b.HasIndex(e => e.ProjectId).IsUnique();
            b.HasOne(e => e.Project)
                .WithOne(p => p.ContentPolicy)
                .HasForeignKey<ContentPolicy>(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProviderHealth>(b =>
        {
            b.ToTable("provider_health_checks");
            b.HasKey(e => e.Id);
            b.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(e => e.ModelCode).HasMaxLength(120);
            b.HasIndex(e => e.CreatedAt);
            b.HasOne(e => e.Provider)
                .WithMany(p => p.HealthChecks)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                if (entry.Entity.CreatedBy == null && currentUserId.HasValue)
                {
                    entry.Entity.CreatedBy = currentUserId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (currentUserId.HasValue)
                {
                    entry.Entity.UpdatedBy = currentUserId;
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
