using AiGatewayApi.Domain.Common;
using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Domain.Entities;

/// <summary>Đếm usage theo kỳ — tránh race khi cộng trực tiếp trên Quota.</summary>
public class QuotaUsage : BaseEntity
{
    public Guid ProjectId { get; set; }
    public QuotaPeriod Period { get; set; } = QuotaPeriod.Monthly;
    public DateOnly PeriodStart { get; set; }
    public long Tokens { get; set; }
    public int Requests { get; set; }
    public decimal CostUsd { get; set; }

    public Project Project { get; set; } = null!;
}
