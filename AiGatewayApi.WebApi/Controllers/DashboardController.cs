using AiGatewayApi.Domain.Enums;
using AiGatewayApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var totalProviders = await _dbContext.AiProviders.CountAsync(p => p.IsActive, ct);
        var totalModels = await _dbContext.AiModels.CountAsync(m => m.IsActive, ct);
        var totalProjects = await _dbContext.Projects.CountAsync(p => p.IsActive, ct);
        var totalActiveKeys = await _dbContext.ApiKeys.CountAsync(k => k.IsActive && k.RevokedAt == null, ct);

        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var logsThisMonth = _dbContext.UsageLogs.Where(l => l.CreatedAt >= monthStart);

        var totalRequests = await logsThisMonth.CountAsync(ct);
        var totalTokens = await logsThisMonth.SumAsync(l => (long)l.TotalTokens, ct);
        var totalCostUsd = await logsThisMonth.SumAsync(l => l.CostUsd, ct);
        var avgLatency = totalRequests > 0 ? await logsThisMonth.AverageAsync(l => (double)l.LatencyMs, ct) : 0d;

        var healthyProviders = await _dbContext.AiProviders.CountAsync(p => p.IsActive && p.HealthStatus == ProviderHealthStatus.Healthy, ct);

        return Ok(new
        {
            totalProviders,
            healthyProviders,
            totalModels,
            totalProjects,
            totalActiveKeys,
            currentMonth = new
            {
                totalRequests,
                totalTokens,
                totalCostUsd = Math.Round(totalCostUsd, 4),
                averageLatencyMs = Math.Round(avgLatency, 1)
            }
        });
    }

    [HttpGet("chart-tokens")]
    public async Task<IActionResult> GetTokensChart([FromQuery] int days = 14, CancellationToken ct = default)
    {
        if (days < 1 || days > 90) days = 14;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);

        var rawLogs = await _dbContext.UsageLogs
            .Where(l => l.CreatedAt >= cutoff)
            .Select(l => new { l.CreatedAt, l.InputTokens, l.OutputTokens, l.TotalTokens })
            .ToListAsync(ct);

        var grouped = rawLogs
            .GroupBy(l => l.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key,
                inputTokens = g.Sum(x => (long)x.InputTokens),
                outputTokens = g.Sum(x => (long)x.OutputTokens),
                totalTokens = g.Sum(x => (long)x.TotalTokens),
                requests = g.Count()
            })
            .ToList();

        return Ok(grouped);
    }

    [HttpGet("chart-cost")]
    public async Task<IActionResult> GetCostChart([FromQuery] int days = 14, CancellationToken ct = default)
    {
        if (days < 1 || days > 90) days = 14;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);

        var rawLogs = await _dbContext.UsageLogs
            .Where(l => l.CreatedAt >= cutoff)
            .Select(l => new { l.CreatedAt, l.CostUsd })
            .ToListAsync(ct);

        var grouped = rawLogs
            .GroupBy(l => l.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key,
                costUsd = Math.Round(g.Sum(x => x.CostUsd), 4),
                requests = g.Count()
            })
            .ToList();

        return Ok(grouped);
    }

    [HttpGet("top-models")]
    public async Task<IActionResult> GetTopModels(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        var grouped = await _dbContext.UsageLogs
            .Where(l => l.CreatedAt >= cutoff)
            .GroupBy(l => l.ModelId)
            .Select(g => new
            {
                ModelId = g.Key,
                TotalTokens = g.Sum(x => (long)x.TotalTokens),
                TotalCostUsd = g.Sum(x => x.CostUsd),
                RequestCount = g.Count()
            })
            .OrderByDescending(x => x.TotalTokens)
            .Take(5)
            .ToListAsync(ct);

        var modelIds = grouped.Select(g => g.ModelId).ToList();
        var models = await _dbContext.AiModels.Where(m => modelIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.DisplayName, ct);

        var result = grouped.Select(g => new
        {
            modelId = g.ModelId,
            modelName = models.TryGetValue(g.ModelId, out var name) ? name : "Unknown Model",
            totalTokens = g.TotalTokens,
            totalCostUsd = Math.Round(g.TotalCostUsd, 4),
            requestCount = g.RequestCount
        });

        return Ok(result);
    }

    [HttpGet("providers-health")]
    public async Task<IActionResult> GetProvidersHealth(CancellationToken ct)
    {
        var providers = await _dbContext.AiProviders
            .Where(p => p.IsActive)
            .OrderBy(p => p.Priority)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ProviderType,
                p.Priority,
                p.HealthStatus,
                p.LastHealthAt,
                p.BaseUrl
            })
            .ToListAsync(ct);

        return Ok(providers);
    }
}
