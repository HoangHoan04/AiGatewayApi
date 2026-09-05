using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Domain.Entities;
using AiGatewayApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IApiKeyHashService _keyHashService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        ApplicationDbContext dbContext,
        IApiKeyHashService keyHashService,
        ILogger<ProjectsController> logger)
    {
        _dbContext = dbContext;
        _keyHashService = keyHashService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var projects = await _dbContext.Projects
            .Include(p => p.Quota)
            .Include(p => p.ApiKeys)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                p.Description,
                p.IsActive,
                p.DefaultModelId,
                p.DefaultTemperature,
                ActiveKeyCount = p.ApiKeys.Count(k => k.IsActive && k.RevokedAt == null),
                Quota = p.Quota != null ? new
                {
                    p.Quota.TokenLimit,
                    p.Quota.RequestLimit,
                    p.Quota.CostLimitUsd,
                    p.Quota.CurrentMonthTokens,
                    p.Quota.CurrentMonthRequests,
                    p.Quota.CurrentMonthCostUsd,
                    p.Quota.AlertThreshold,
                    p.Quota.RateLimitRpm,
                    p.Quota.SoftLimit
                } : null,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var p = await _dbContext.Projects
            .Include(x => x.Quota)
            .Include(x => x.ApiKeys)
            .Include(x => x.DefaultModel)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (p == null) return NotFound(new { error = "Project not found" });

        return Ok(new
        {
            p.Id,
            p.Name,
            p.Code,
            p.Description,
            p.IsActive,
            p.DefaultModelId,
            DefaultModelName = p.DefaultModel?.DisplayName,
            p.DefaultTemperature,
            p.AllowedProviderTypes,
            p.CallbackWebhook,
            p.RetentionDays,
            Quota = p.Quota != null ? new
            {
                p.Quota.Id,
                p.Quota.TokenLimit,
                p.Quota.RequestLimit,
                p.Quota.CostLimitUsd,
                p.Quota.CurrentMonthTokens,
                p.Quota.CurrentMonthRequests,
                p.Quota.CurrentMonthCostUsd,
                p.Quota.AlertThreshold,
                p.Quota.RateLimitRpm,
                p.Quota.RateLimitTpd,
                p.Quota.SoftLimit,
                p.Quota.AlertWebhook,
                p.Quota.LastAlertedAt
            } : null,
            ApiKeys = p.ApiKeys.Select(k => new
            {
                k.Id,
                k.Name,
                k.KeyPrefix,
                k.RateLimitRpm,
                k.AllowedModels,
                k.ExpiresAt,
                k.LastUsedAt,
                k.RevokedAt,
                k.IsActive,
                k.CreatedAt
            }),
            p.CreatedAt,
            p.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var existing = await _dbContext.Projects.AnyAsync(x => x.Code == request.Code, ct);
        if (existing) return BadRequest(new { error = $"Project with code '{request.Code}' already exists." });

        var project = new Project
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToLowerInvariant(),
            Description = request.Description,
            DefaultModelId = request.DefaultModelId,
            DefaultTemperature = request.DefaultTemperature,
            IsActive = request.IsActive
        };

        _dbContext.Projects.Add(project);

        var quota = new Quota
        {
            ProjectId = project.Id,
            TokenLimit = request.TokenLimit ?? 10_000_000,
            RequestLimit = request.RequestLimit ?? 50_000,
            CostLimitUsd = request.CostLimitUsd ?? 100.00m,
            AlertThreshold = request.AlertThreshold ?? 80.00m,
            RateLimitRpm = request.RateLimitRpm ?? 120,
            SoftLimit = request.SoftLimit
        };

        _dbContext.Quotas.Add(quota);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, new { project.Id, project.Name, project.Code });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await _dbContext.Projects.FindAsync(new object[] { id }, ct);
        if (project == null) return NotFound(new { error = "Project not found" });

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.DefaultModelId = request.DefaultModelId;
        project.DefaultTemperature = request.DefaultTemperature;
        project.AllowedProviderTypes = request.AllowedProviderTypes;
        project.CallbackWebhook = request.CallbackWebhook;
        project.RetentionDays = request.RetentionDays;
        project.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { project.Id, project.Name, updated = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var project = await _dbContext.Projects
            .Include(p => p.ApiKeys)
            .Include(p => p.Quota)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (project == null) return NotFound(new { error = "Project not found" });

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Project deleted successfully." });
    }

    [HttpPost("{id:guid}/keys")]
    public async Task<IActionResult> GenerateKey(Guid id, [FromBody] GenerateKeyRequest request, CancellationToken ct)
    {
        var project = await _dbContext.Projects.FindAsync(new object[] { id }, ct);
        if (project == null) return NotFound(new { error = "Project not found" });

        var (rawKey, prefix) = _keyHashService.GenerateKey(project.Code);
        var hash = _keyHashService.HashKey(rawKey);

        var apiKey = new ApiKey
        {
            ProjectId = project.Id,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{project.Code} API Key" : request.Name.Trim(),
            KeyPrefix = prefix,
            KeyHash = hash,
            AllowedModels = request.AllowedModels,
            RateLimitRpm = request.RateLimitRpm ?? 120,
            ExpiresAt = request.ExpiresInDays.HasValue ? DateTimeOffset.UtcNow.AddDays(request.ExpiresInDays.Value) : null,
            IsActive = true
        };

        _dbContext.ApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new
        {
            keyId = apiKey.Id,
            apiKey = rawKey, // Plaintext returned ONLY ONCE!
            prefix = apiKey.KeyPrefix,
            name = apiKey.Name,
            expiresAt = apiKey.ExpiresAt,
            rateLimitRpm = apiKey.RateLimitRpm,
            warning = "IMPORTANT: Please copy your API Key now. You will NOT be able to view it again."
        });
    }

    [HttpDelete("{id:guid}/keys/{keyId:guid}")]
    public async Task<IActionResult> RevokeKey(Guid id, Guid keyId, [FromQuery] string? reason, CancellationToken ct)
    {
        var key = await _dbContext.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.ProjectId == id, ct);
        if (key == null) return NotFound(new { error = "API Key not found" });

        key.RevokedAt = DateTimeOffset.UtcNow;
        key.IsActive = false;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "API Key revoked successfully." });
    }

    [HttpPut("{id:guid}/quota")]
    public async Task<IActionResult> UpdateQuota(Guid id, [FromBody] UpdateQuotaRequest request, CancellationToken ct)
    {
        var quota = await _dbContext.Quotas.FirstOrDefaultAsync(q => q.ProjectId == id, ct);
        if (quota == null)
        {
            quota = new Quota { ProjectId = id };
            _dbContext.Quotas.Add(quota);
        }

        quota.TokenLimit = request.TokenLimit;
        quota.RequestLimit = request.RequestLimit;
        quota.CostLimitUsd = request.CostLimitUsd;
        quota.AlertThreshold = request.AlertThreshold;
        quota.RateLimitRpm = request.RateLimitRpm;
        quota.SoftLimit = request.SoftLimit;
        quota.AlertWebhook = request.AlertWebhook;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { success = true, quota });
    }
}

public record CreateProjectRequest(
    string Name,
    string Code,
    string? Description,
    Guid? DefaultModelId,
    decimal? DefaultTemperature,
    long? TokenLimit,
    int? RequestLimit,
    decimal? CostLimitUsd,
    decimal? AlertThreshold,
    int? RateLimitRpm,
    bool SoftLimit = false,
    bool IsActive = true
);

public record UpdateProjectRequest(
    string Name,
    string? Description,
    Guid? DefaultModelId,
    decimal? DefaultTemperature,
    string? AllowedProviderTypes,
    string? CallbackWebhook,
    int RetentionDays,
    bool IsActive
);

public record GenerateKeyRequest(
    string? Name,
    string? AllowedModels,
    int? RateLimitRpm,
    int? ExpiresInDays
);

public record UpdateQuotaRequest(
    long? TokenLimit,
    int? RequestLimit,
    decimal? CostLimitUsd,
    decimal AlertThreshold,
    int? RateLimitRpm,
    bool SoftLimit,
    string? AlertWebhook
);
