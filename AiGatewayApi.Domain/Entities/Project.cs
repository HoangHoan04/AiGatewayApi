using AiGatewayApi.Domain.Common;

namespace AiGatewayApi.Domain.Entities;

public class Project : BaseEntity, ITenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DefaultModelId { get; set; }
    public Guid? CompanyId { get; set; }
    public decimal? DefaultTemperature { get; set; }
    public string? AllowedProviderTypes { get; set; }
    public string? CallbackWebhook { get; set; }
    public int RetentionDays { get; set; } = 0;

    public AiModel? DefaultModel { get; set; }
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
    public Quota? Quota { get; set; }
    public ICollection<QuotaUsage> QuotaUsages { get; set; } = new List<QuotaUsage>();
    public ICollection<PromptTemplate> PromptTemplates { get; set; } = new List<PromptTemplate>();
    public ICollection<RoutingPolicy> RoutingPolicies { get; set; } = new List<RoutingPolicy>();
    public ICollection<AsyncJob> AsyncJobs { get; set; } = new List<AsyncJob>();
    public ContentPolicy? ContentPolicy { get; set; }
}
