namespace AiGatewayApi.Application.Common.DTOs;

public record DashboardSummaryDto(
    int TodayRequests,
    int TodaySuccessRequests,
    decimal TodayCostUsd,
    int ThisMonthRequests,
    decimal ThisMonthCostUsd,
    int ActiveProjects,
    int ActiveProviders,
    double OverallSuccessRate,
    double AvgLatencyMs,
    List<DailyUsageDto> DailyTrend,
    List<ProjectUsageDto> ProjectBreakdown,
    List<ProviderUsageDto> ProviderBreakdown);

public record DailyUsageDto(
    DateOnly Date,
    int RequestCount,
    int TokenCount,
    decimal CostUsd);

public record ProjectUsageDto(
    string ProjectCode,
    string ProjectName,
    int RequestCount,
    decimal CostUsd,
    double Percentage);

public record ProviderUsageDto(
    string ProviderType,
    int RequestCount,
    decimal CostUsd,
    double Percentage);
