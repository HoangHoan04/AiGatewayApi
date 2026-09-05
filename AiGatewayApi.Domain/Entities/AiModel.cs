using AiGatewayApi.Domain.Common;
using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Domain.Entities;

public class AiModel : BaseEntity
{
    public Guid ProviderId { get; set; }
    public string ModelCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal InputPricePer1K { get; set; }
    public decimal OutputPricePer1K { get; set; }
    public decimal? OutputPricePer1KCached { get; set; }
    public PriceUnit PriceUnit { get; set; } = PriceUnit.Per1KTokens;
    public int MaxContextTokens { get; set; } = 4096;
    public bool SupportsStreaming { get; set; } = true;
    public AiModelCapability Capabilities { get; set; } = AiModelCapability.Chat;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? DeprecatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public AiProvider Provider { get; set; } = null!;
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
    public ICollection<RoutingPolicy> RoutingPolicies { get; set; } = new List<RoutingPolicy>();
}
