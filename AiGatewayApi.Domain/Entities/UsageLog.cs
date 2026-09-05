using AiGatewayApi.Domain.Common;
using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Domain.Entities;

public class UsageLog : ImmutableLogEntity, ITenantEntity
{
    public Guid ProjectId { get; set; }
    public Guid ApiKeyId { get; set; }
    public Guid ModelId { get; set; }
    public Guid? CompanyId { get; set; }
    public AiProviderType ProviderType { get; set; }
    public AiProviderType? FallbackFromProvider { get; set; }
    public AiEndpointType Endpoint { get; set; } = AiEndpointType.Chat;
    public Guid? PromptTemplateId { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal CostUsd { get; set; }
    public int LatencyMs { get; set; }
    public UsageStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? RequestId { get; set; }
    public bool IsStreaming { get; set; }
    public bool IsBillable { get; set; } = true;

    public Project Project { get; set; } = null!;
    public ApiKey ApiKey { get; set; } = null!;
    public AiModel Model { get; set; } = null!;
    public PromptTemplate? PromptTemplate { get; set; }
}
