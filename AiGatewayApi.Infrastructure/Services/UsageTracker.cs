using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Domain.Entities;
using AiGatewayApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiGatewayApi.Infrastructure.Services;

public class UsageTracker : IUsageTracker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UsageTracker> _logger;

    public UsageTracker(IServiceScopeFactory scopeFactory, ILogger<UsageTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task TrackAsync(UsageRecord record, CancellationToken ct = default)
    {
        // Fire-and-forget inside task scope so it never blocks the caller
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var model = await db.AiModels.AsNoTracking().FirstOrDefaultAsync(m => m.Id == record.ModelId);
                decimal cost = 0;
                if (model != null)
                {
                    var inRate = model.InputPricePer1K / 1000m;
                    var outRate = model.OutputPricePer1K / 1000m;
                    cost = (record.InputTokens * inRate) + (record.OutputTokens * outRate);
                }

                var log = new UsageLog
                {
                    Id = Guid.NewGuid(),
                    ProjectId = record.ProjectId,
                    ApiKeyId = record.ApiKeyId,
                    ModelId = record.ModelId,
                    ProviderType = record.ProviderType,
                    InputTokens = record.InputTokens,
                    OutputTokens = record.OutputTokens,
                    CostUsd = cost,
                    LatencyMs = record.LatencyMs,
                    Status = record.Status,
                    IsStreaming = record.IsStreaming,
                    RequestId = record.RequestId ?? Guid.NewGuid().ToString("N"),
                    ErrorCode = record.ErrorCode,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                db.UsageLogs.Add(log);

                // Update quota usage
                var quota = await db.Quotas.FirstOrDefaultAsync(q => q.ProjectId == record.ProjectId);
                if (quota != null)
                {
                    quota.CurrentMonthTokens += record.InputTokens + record.OutputTokens;
                    quota.CurrentMonthRequests += 1;
                    quota.UpdatedAt = DateTimeOffset.UtcNow;
                }

                // Update API key last used
                var apiKey = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == record.ApiKeyId);
                if (apiKey != null)
                {
                    apiKey.LastUsedAt = DateTimeOffset.UtcNow;
                    apiKey.UpdatedAt = DateTimeOffset.UtcNow;
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to track AI usage for project {ProjectId}", record.ProjectId);
            }
        });
    }
}
