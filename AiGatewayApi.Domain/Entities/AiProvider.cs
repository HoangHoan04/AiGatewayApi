using AiGatewayApi.Domain.Common;
using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Domain.Entities;

public class AiProvider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public AiProviderType ProviderType { get; set; }
    public string? BaseUrl { get; set; }
    public string ApiKeyEncrypted { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public int Priority { get; set; }
    public int TimeoutMs { get; set; } = 60000;
    public int MaxRetries { get; set; } = 2;
    public string? HeadersJson { get; set; }
    public string? OrganizationId { get; set; }
    public string? AzureDeployment { get; set; }
    public bool IsDefaultFallback { get; set; }
    public DateTimeOffset? LastHealthAt { get; set; }
    public ProviderHealthStatus HealthStatus { get; set; } = ProviderHealthStatus.Unknown;

    public ICollection<AiModel> Models { get; set; } = new List<AiModel>();
    public ICollection<ProviderHealth> HealthChecks { get; set; } = new List<ProviderHealth>();
    public ICollection<RoutingPolicy> RoutingPolicies { get; set; } = new List<RoutingPolicy>();
}
