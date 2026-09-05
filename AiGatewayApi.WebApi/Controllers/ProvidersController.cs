using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Domain.Entities;
using AiGatewayApi.Domain.Enums;
using AiGatewayApi.Infrastructure.Persistence;
using AiGatewayApi.Infrastructure.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/providers")]
public class ProvidersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly ILlmClientFactory _clientFactory;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(
        ApplicationDbContext dbContext,
        IEncryptionService encryptionService,
        ILlmClientFactory clientFactory,
        ILogger<ProvidersController> logger)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var providers = await _dbContext.AiProviders
            .Include(p => p.Models)
            .OrderBy(p => p.Priority)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ProviderType,
                p.BaseUrl,
                p.Priority,
                p.TimeoutMs,
                p.MaxRetries,
                p.IsActive,
                p.Notes,
                p.HealthStatus,
                p.LastHealthAt,
                HasApiKey = !string.IsNullOrWhiteSpace(p.ApiKeyEncrypted),
                ModelCount = p.Models.Count(m => m.IsActive),
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(providers);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var provider = await _dbContext.AiProviders
            .Include(p => p.Models)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (provider == null) return NotFound(new { error = "Provider not found" });

        return Ok(new
        {
            provider.Id,
            provider.Name,
            provider.ProviderType,
            provider.BaseUrl,
            provider.Priority,
            provider.TimeoutMs,
            provider.MaxRetries,
            provider.IsActive,
            provider.Notes,
            provider.HealthStatus,
            provider.LastHealthAt,
            provider.OrganizationId,
            provider.AzureDeployment,
            provider.HeadersJson,
            HasApiKey = !string.IsNullOrWhiteSpace(provider.ApiKeyEncrypted),
            Models = provider.Models.Select(m => new
            {
                m.Id,
                m.ModelCode,
                m.DisplayName,
                m.InputPricePer1K,
                m.OutputPricePer1K,
                m.MaxContextTokens,
                m.SupportsStreaming,
                m.Capabilities,
                m.IsDefault,
                m.IsActive
            }),
            provider.CreatedAt,
            provider.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProviderRequest request, CancellationToken ct)
    {
        var provider = new AiProvider
        {
            Name = request.Name,
            ProviderType = request.ProviderType,
            BaseUrl = request.BaseUrl,
            ApiKeyEncrypted = !string.IsNullOrWhiteSpace(request.ApiKey) ? _encryptionService.Encrypt(request.ApiKey) : string.Empty,
            Priority = request.Priority,
            TimeoutMs = request.TimeoutMs > 0 ? request.TimeoutMs : 60000,
            MaxRetries = request.MaxRetries >= 0 ? request.MaxRetries : 2,
            Notes = request.Notes,
            OrganizationId = request.OrganizationId,
            AzureDeployment = request.AzureDeployment,
            HeadersJson = request.HeadersJson,
            IsActive = request.IsActive
        };

        _dbContext.AiProviders.Add(provider);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = provider.Id }, new { provider.Id, provider.Name });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProviderRequest request, CancellationToken ct)
    {
        var provider = await _dbContext.AiProviders.FindAsync(new object[] { id }, ct);
        if (provider == null) return NotFound(new { error = "Provider not found" });

        provider.Name = request.Name;
        provider.ProviderType = request.ProviderType;
        provider.BaseUrl = request.BaseUrl;
        provider.Priority = request.Priority;
        provider.TimeoutMs = request.TimeoutMs;
        provider.MaxRetries = request.MaxRetries;
        provider.Notes = request.Notes;
        provider.OrganizationId = request.OrganizationId;
        provider.AzureDeployment = request.AzureDeployment;
        provider.HeadersJson = request.HeadersJson;
        provider.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            provider.ApiKeyEncrypted = _encryptionService.Encrypt(request.ApiKey);
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { provider.Id, provider.Name, updated = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var provider = await _dbContext.AiProviders
            .Include(p => p.Models)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (provider == null) return NotFound(new { error = "Provider not found" });

        _dbContext.AiProviders.Remove(provider);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Provider removed successfully" });
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct)
    {
        var provider = await _dbContext.AiProviders
            .Include(p => p.Models)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (provider == null) return NotFound(new { error = "Provider not found" });

        var client = _clientFactory.GetClient(provider.ProviderType);
        var decryptedKey = !string.IsNullOrWhiteSpace(provider.ApiKeyEncrypted)
            ? _encryptionService.Decrypt(provider.ApiKeyEncrypted)
            : string.Empty;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool success = false;
        string message = "Connection successful";
        try
        {
            success = await client.PingHealthAsync(provider, decryptedKey, ct);
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
        }
        sw.Stop();

        provider.LastHealthAt = DateTimeOffset.UtcNow;
        provider.HealthStatus = success ? ProviderHealthStatus.Healthy : ProviderHealthStatus.Unhealthy;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new
        {
            providerId = provider.Id,
            providerName = provider.Name,
            success,
            latencyMs = sw.ElapsedMilliseconds,
            message,
            testedAt = provider.LastHealthAt
        });
    }
}

public record CreateProviderRequest(
    string Name,
    AiProviderType ProviderType,
    string? BaseUrl,
    string? ApiKey,
    int Priority,
    int TimeoutMs,
    int MaxRetries,
    string? Notes,
    string? OrganizationId,
    string? AzureDeployment,
    string? HeadersJson,
    bool IsActive = true
);

public record UpdateProviderRequest(
    string Name,
    AiProviderType ProviderType,
    string? BaseUrl,
    string? ApiKey,
    int Priority,
    int TimeoutMs,
    int MaxRetries,
    string? Notes,
    string? OrganizationId,
    string? AzureDeployment,
    string? HeadersJson,
    bool IsActive
);
