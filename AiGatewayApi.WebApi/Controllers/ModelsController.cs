using AiGatewayApi.Domain.Entities;
using AiGatewayApi.Domain.Enums;
using AiGatewayApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/models")]
public class ModelsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(ApplicationDbContext dbContext, ILogger<ModelsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? providerId, [FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var query = _dbContext.AiModels
            .Include(m => m.Provider)
            .AsNoTracking();

        if (providerId.HasValue)
        {
            query = query.Where(m => m.ProviderId == providerId.Value);
        }

        if (activeOnly == true)
        {
            query = query.Where(m => m.IsActive);
        }

        var list = await query
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.DisplayName)
            .Select(m => new
            {
                m.Id,
                m.ProviderId,
                ProviderName = m.Provider.Name,
                m.Provider.ProviderType,
                m.ModelCode,
                m.DisplayName,
                m.InputPricePer1K,
                m.OutputPricePer1K,
                InputPricePer1M = m.InputPricePer1K * 1000m,
                OutputPricePer1M = m.OutputPricePer1K * 1000m,
                m.PriceUnit,
                m.MaxContextTokens,
                m.SupportsStreaming,
                m.Capabilities,
                m.IsDefault,
                m.SortOrder,
                m.IsActive,
                m.CreatedAt,
                m.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var m = await _dbContext.AiModels
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (m == null) return NotFound(new { error = "Model not found" });

        return Ok(new
        {
            m.Id,
            m.ProviderId,
            ProviderName = m.Provider.Name,
            m.ModelCode,
            m.DisplayName,
            m.InputPricePer1K,
            m.OutputPricePer1K,
            InputPricePer1M = m.InputPricePer1K * 1000m,
            OutputPricePer1M = m.OutputPricePer1K * 1000m,
            m.PriceUnit,
            m.MaxContextTokens,
            m.SupportsStreaming,
            m.Capabilities,
            m.IsDefault,
            m.SortOrder,
            m.IsActive,
            m.CreatedAt,
            m.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModelRequest request, CancellationToken ct)
    {
        var provider = await _dbContext.AiProviders.FindAsync(new object[] { request.ProviderId }, ct);
        if (provider == null) return BadRequest(new { error = "Target provider does not exist." });

        if (request.IsDefault)
        {
            // Reset existing defaults
            var existingDefaults = await _dbContext.AiModels.Where(x => x.IsDefault).ToListAsync(ct);
            foreach (var d in existingDefaults) d.IsDefault = false;
        }

        var model = new AiModel
        {
            ProviderId = request.ProviderId,
            ModelCode = request.ModelCode.Trim(),
            DisplayName = request.DisplayName.Trim(),
            InputPricePer1K = request.InputPricePer1K,
            OutputPricePer1K = request.OutputPricePer1K,
            MaxContextTokens = request.MaxContextTokens > 0 ? request.MaxContextTokens : 4096,
            SupportsStreaming = request.SupportsStreaming,
            Capabilities = request.Capabilities,
            IsDefault = request.IsDefault,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        _dbContext.AiModels.Add(model);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = model.Id }, new { model.Id, model.ModelCode, model.DisplayName });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateModelRequest request, CancellationToken ct)
    {
        var model = await _dbContext.AiModels.FindAsync(new object[] { id }, ct);
        if (model == null) return NotFound(new { error = "Model not found" });

        if (request.IsDefault && !model.IsDefault)
        {
            var existingDefaults = await _dbContext.AiModels.Where(x => x.IsDefault && x.Id != id).ToListAsync(ct);
            foreach (var d in existingDefaults) d.IsDefault = false;
        }

        model.DisplayName = request.DisplayName;
        model.InputPricePer1K = request.InputPricePer1K;
        model.OutputPricePer1K = request.OutputPricePer1K;
        model.MaxContextTokens = request.MaxContextTokens;
        model.SupportsStreaming = request.SupportsStreaming;
        model.Capabilities = request.Capabilities;
        model.IsDefault = request.IsDefault;
        model.SortOrder = request.SortOrder;
        model.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { model.Id, model.ModelCode, updated = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var model = await _dbContext.AiModels.FindAsync(new object[] { id }, ct);
        if (model == null) return NotFound(new { error = "Model not found" });

        _dbContext.AiModels.Remove(model);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Model removed successfully" });
    }
}

public record CreateModelRequest(
    Guid ProviderId,
    string ModelCode,
    string DisplayName,
    decimal InputPricePer1K,
    decimal OutputPricePer1K,
    int MaxContextTokens,
    bool SupportsStreaming,
    AiModelCapability Capabilities,
    bool IsDefault,
    int SortOrder,
    bool IsActive = true
);

public record UpdateModelRequest(
    string DisplayName,
    decimal InputPricePer1K,
    decimal OutputPricePer1K,
    int MaxContextTokens,
    bool SupportsStreaming,
    AiModelCapability Capabilities,
    bool IsDefault,
    int SortOrder,
    bool IsActive
);
