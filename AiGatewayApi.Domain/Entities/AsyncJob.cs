using AiGatewayApi.Domain.Common;
using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Domain.Entities;

public class AsyncJob : BaseEntity, ITenantEntity
{
    public Guid ProjectId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? ApiKeyId { get; set; }
    public AiEndpointType Endpoint { get; set; } = AiEndpointType.Ocr;
    public AsyncJobStatus Status { get; set; } = AsyncJobStatus.Pending;
    public int Progress { get; set; }
    public string? InputRef { get; set; }
    public string? ResultRef { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CallbackWebhook { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ApiKey? ApiKey { get; set; }
}
