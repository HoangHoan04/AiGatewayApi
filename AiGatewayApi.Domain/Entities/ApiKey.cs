using AiGatewayApi.Domain.Common;

namespace AiGatewayApi.Domain.Entities;

public class ApiKey : BaseEntity, ITenantEntity
{
    public Guid ProjectId { get; set; }
    public Guid? CompanyId { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AllowedModels { get; set; }
    public string? Scopes { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public int? RateLimitRpm { get; set; }
    public bool IsActive { get; set; } = true;

    public Project Project { get; set; } = null!;
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
}
