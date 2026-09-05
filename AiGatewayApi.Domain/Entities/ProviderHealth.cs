using AiGatewayApi.Domain.Common;
using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Domain.Entities;

public class ProviderHealth : ImmutableLogEntity
{
    public Guid ProviderId { get; set; }
    public ProviderHealthStatus Status { get; set; } = ProviderHealthStatus.Unknown;
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ModelCode { get; set; }

    public AiProvider Provider { get; set; } = null!;
}
