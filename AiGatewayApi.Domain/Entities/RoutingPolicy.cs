using AiGatewayApi.Domain.Common;

namespace AiGatewayApi.Domain.Entities;

/// <summary>Thứ tự model/provider fallback cho một project.</summary>
public class RoutingPolicy : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? ProviderId { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConditionJson { get; set; }

    public Project Project { get; set; } = null!;
    public AiModel? Model { get; set; }
    public AiProvider? Provider { get; set; }
}
