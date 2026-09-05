using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Application.Common.Interfaces;

public record UsageRecord(
    Guid ProjectId,
    Guid ApiKeyId,
    Guid ModelId,
    AiProviderType ProviderType,
    int InputTokens,
    int OutputTokens,
    int LatencyMs,
    UsageStatus Status,
    bool IsStreaming,
    string? RequestId = null,
    string? ErrorCode = null);

/// <summary>
/// Ghi usage_logs sau m?i proxy call. Fire-and-forget, không block response.
/// </summary>
public interface IUsageTracker
{
    Task TrackAsync(UsageRecord record, CancellationToken ct = default);
}
