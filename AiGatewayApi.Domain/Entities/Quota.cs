using AiGatewayApi.Domain.Common;

namespace AiGatewayApi.Domain.Entities;

public class Quota : BaseEntity
{
    public Guid ProjectId { get; set; }
    public long? TokenLimit { get; set; }
    public int? RequestLimit { get; set; }
    public int? RateLimitRpm { get; set; }
    public long? RateLimitTpd { get; set; }
    public decimal? CostLimitUsd { get; set; }
    public bool SoftLimit { get; set; }
    public long CurrentMonthTokens { get; set; }
    public int CurrentMonthRequests { get; set; }
    public decimal CurrentMonthCostUsd { get; set; }
    public decimal AlertThreshold { get; set; } = 80.00m;
    public string? AlertWebhook { get; set; }
    public DateTimeOffset? LastAlertedAt { get; set; }

    public Project Project { get; set; } = null!;
}
