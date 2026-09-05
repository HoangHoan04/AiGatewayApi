using AiGatewayApi.Domain.Enums;
using AiGatewayApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/logs")]
public class UsageLogsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public UsageLogsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid? projectId,
        [FromQuery] AiProviderType? providerType,
        [FromQuery] AiEndpointType? endpoint,
        [FromQuery] UsageStatus? status,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _dbContext.UsageLogs
            .Include(l => l.Project)
            .Include(l => l.Model)
            .Include(l => l.ApiKey)
            .AsNoTracking();

        if (projectId.HasValue) query = query.Where(l => l.ProjectId == projectId.Value);
        if (providerType.HasValue) query = query.Where(l => l.ProviderType == providerType.Value);
        if (endpoint.HasValue) query = query.Where(l => l.Endpoint == endpoint.Value);
        if (status.HasValue) query = query.Where(l => l.Status == status.Value);
        if (fromDate.HasValue) query = query.Where(l => l.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(l => l.CreatedAt <= toDate.Value);

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.CreatedAt,
                l.ProjectId,
                ProjectName = l.Project.Name,
                ProjectCode = l.Project.Code,
                l.ApiKeyId,
                KeyPrefix = l.ApiKey.KeyPrefix,
                ModelCode = l.Model.ModelCode,
                ModelDisplayName = l.Model.DisplayName,
                l.ProviderType,
                l.FallbackFromProvider,
                l.Endpoint,
                l.InputTokens,
                l.OutputTokens,
                l.TotalTokens,
                l.CostUsd,
                l.LatencyMs,
                l.Status,
                l.ErrorCode,
                l.RequestId,
                l.IsStreaming
            })
            .ToListAsync(ct);

        return Ok(new
        {
            items,
            page,
            pageSize,
            totalItems,
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid? projectId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken ct = default)
    {
        var query = _dbContext.UsageLogs.AsNoTracking();

        if (projectId.HasValue) query = query.Where(l => l.ProjectId == projectId.Value);
        if (fromDate.HasValue) query = query.Where(l => l.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(l => l.CreatedAt <= toDate.Value);

        var totalRequests = await query.CountAsync(ct);
        if (totalRequests == 0)
        {
            return Ok(new
            {
                totalRequests = 0,
                successfulRequests = 0,
                failedRequests = 0,
                totalTokens = 0L,
                inputTokens = 0L,
                outputTokens = 0L,
                totalCostUsd = 0m,
                averageLatencyMs = 0d,
                successRate = 100.0
            });
        }

        var successfulRequests = await query.CountAsync(l => l.Status == UsageStatus.Success, ct);
        var totalTokens = await query.SumAsync(l => (long)l.TotalTokens, ct);
        var inputTokens = await query.SumAsync(l => (long)l.InputTokens, ct);
        var outputTokens = await query.SumAsync(l => (long)l.OutputTokens, ct);
        var totalCostUsd = await query.SumAsync(l => l.CostUsd, ct);
        var avgLatency = await query.AverageAsync(l => (double)l.LatencyMs, ct);

        return Ok(new
        {
            totalRequests,
            successfulRequests,
            failedRequests = totalRequests - successfulRequests,
            totalTokens,
            inputTokens,
            outputTokens,
            totalCostUsd,
            averageLatencyMs = Math.Round(avgLatency, 1),
            successRate = Math.Round((successfulRequests / (double)totalRequests) * 100, 2)
        });
    }
}
