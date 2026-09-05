namespace AiGatewayApi.Application.Common.DTOs;

public record QuotaDto(
    Guid Id,
    Guid ProjectId,
    long? TokenLimit,
    int? RequestLimit,
    int? RateLimitRpm,
    long? RateLimitTpd,
    long CurrentMonthTokens,
    int CurrentMonthRequests,
    decimal AlertThreshold,
    string? AlertWebhook,
    double? TokenUsagePercent,
    double? RequestUsagePercent);

public record UpdateQuotaRequest(
    Guid ProjectId,
    long? TokenLimit,
    int? RequestLimit,
    int? RateLimitRpm,
    long? RateLimitTpd,
    decimal AlertThreshold,
    string? AlertWebhook);
