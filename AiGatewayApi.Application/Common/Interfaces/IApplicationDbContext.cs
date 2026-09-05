using AiGatewayApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AiProvider> AiProviders { get; }
    DbSet<AiModel> AiModels { get; }
    DbSet<Project> Projects { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<UsageLog> UsageLogs { get; }
    DbSet<Quota> Quotas { get; }
    DbSet<QuotaUsage> QuotaUsages { get; }
    DbSet<PromptTemplate> PromptTemplates { get; }
    DbSet<PromptTemplateVersion> PromptTemplateVersions { get; }
    DbSet<RoutingPolicy> RoutingPolicies { get; }
    DbSet<AsyncJob> AsyncJobs { get; }
    DbSet<ContentPolicy> ContentPolicies { get; }
    DbSet<ProviderHealth> ProviderHealthChecks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
