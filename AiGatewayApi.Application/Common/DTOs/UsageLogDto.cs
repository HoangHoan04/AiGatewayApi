using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Application.Common.DTOs;

public record UsageLogDto(
    Guid Id,
    Guid ProjectId,
    string ProjectCode,
    Guid ApiKeyId,
    string ApiKeyPrefix,
    Guid ModelId,
    string ModelCode,
    AiProviderType ProviderType,
    int InputTokens,
    int OutputTokens,
    int TotalTokens,
    decimal CostUsd,
    int LatencyMs,
    UsageStatus Status,
    string? ErrorCode,
    string? RequestId,
    bool IsStreaming,
    DateTimeOffset CreatedAt);

public record ListUsageLogsRequest(
    Guid? ProjectId,
    Guid? ModelId,
    AiProviderType? ProviderType,
    UsageStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageIndex = 0,
    int PageSize = 20);

public record PagedResult<T>(
    List<T> Items,
    int PageIndex,
    int PageSize,
    int TotalCount);
