using System.Text.RegularExpressions;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Domain.Entities;
using AiGatewayApi.Infrastructure.Persistence;
using AiGatewayApi.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/templates")]
public class PromptTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILlmRouterService _routerService;
    private readonly IGatewayKeyContext _gatewayContext;
    private readonly ILogger<PromptTemplatesController> _logger;

    public PromptTemplatesController(
        ApplicationDbContext dbContext,
        ILlmRouterService routerService,
        IGatewayKeyContext gatewayContext,
        ILogger<PromptTemplatesController> logger)
    {
        _dbContext = dbContext;
        _routerService = routerService;
        _gatewayContext = gatewayContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, [FromQuery] string? category, CancellationToken ct)
    {
        var query = _dbContext.PromptTemplates
            .Include(t => t.PublishedVersion)
            .Include(t => t.Project)
            .AsNoTracking();

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Module == category || t.SourceSystem == category);
        }

        var list = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : null,
                t.Code,
                t.Name,
                t.Description,
                t.SourceSystem,
                t.Module,
                t.VariablesSchemaJson,
                t.PublishedVersionId,
                PublishedVersionNumber = t.PublishedVersion != null ? t.PublishedVersion.VersionNumber : (int?)null,
                SystemPrompt = t.PublishedVersion != null ? t.PublishedVersion.SystemPrompt : null,
                UserPromptTemplate = t.PublishedVersion != null ? t.PublishedVersion.UserPromptTemplate : null,
                t.IsActive,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var t = await _dbContext.PromptTemplates
            .Include(x => x.PublishedVersion)
            .Include(x => x.Project)
            .Include(x => x.Versions.OrderByDescending(v => v.VersionNumber))
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (t == null) return NotFound(new { error = "Template not found" });

        return Ok(new
        {
            t.Id,
            t.ProjectId,
            ProjectName = t.Project?.Name,
            t.Code,
            t.Name,
            t.Description,
            t.SourceSystem,
            t.Module,
            t.VariablesSchemaJson,
            t.PublishedVersionId,
            PublishedVersion = t.PublishedVersion != null ? new
            {
                t.PublishedVersion.Id,
                t.PublishedVersion.VersionNumber,
                t.PublishedVersion.SystemPrompt,
                t.PublishedVersion.UserPromptTemplate,
                t.PublishedVersion.PublishedAt,
                t.PublishedVersion.ChangeNote
            } : null,
            Versions = t.Versions.Select(v => new
            {
                v.Id,
                v.VersionNumber,
                v.SystemPrompt,
                v.UserPromptTemplate,
                v.IsPublished,
                v.PublishedAt,
                v.ChangeNote,
                v.CreatedAt
            }),
            t.IsActive,
            t.CreatedAt,
            t.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        var existing = await _dbContext.PromptTemplates.AnyAsync(t => t.Code == request.Code, ct);
        if (existing) return BadRequest(new { error = $"Template code '{request.Code}' already exists." });

        var template = new PromptTemplate
        {
            ProjectId = request.ProjectId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            SourceSystem = request.SourceSystem,
            Module = request.Module,
            VariablesSchemaJson = request.VariablesSchemaJson,
            IsActive = true
        };

        _dbContext.PromptTemplates.Add(template);

        var version = new PromptTemplateVersion
        {
            TemplateId = template.Id,
            VersionNumber = 1,
            SystemPrompt = request.SystemPrompt ?? string.Empty,
            UserPromptTemplate = request.UserPromptTemplate,
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow,
            ChangeNote = "Initial version"
        };

        _dbContext.PromptTemplateVersions.Add(version);
        template.PublishedVersionId = version.Id;

        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = template.Id }, new { template.Id, template.Code, template.Name });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateRequest request, CancellationToken ct)
    {
        var template = await _dbContext.PromptTemplates.FindAsync(new object[] { id }, ct);
        if (template == null) return NotFound(new { error = "Template not found" });

        template.Name = request.Name.Trim();
        template.Description = request.Description;
        template.SourceSystem = request.SourceSystem;
        template.Module = request.Module;
        template.VariablesSchemaJson = request.VariablesSchemaJson;
        template.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { template.Id, template.Name, updated = true });
    }

    [HttpPost("{id:guid}/versions")]
    public async Task<IActionResult> CreateVersion(Guid id, [FromBody] CreateVersionRequest request, CancellationToken ct)
    {
        var template = await _dbContext.PromptTemplates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template == null) return NotFound(new { error = "Template not found" });

        var maxVersion = template.Versions.Any() ? template.Versions.Max(v => v.VersionNumber) : 0;
        var nextVersion = maxVersion + 1;

        var version = new PromptTemplateVersion
        {
            TemplateId = id,
            VersionNumber = nextVersion,
            SystemPrompt = request.SystemPrompt ?? string.Empty,
            UserPromptTemplate = request.UserPromptTemplate,
            IsPublished = request.PublishImmediately,
            PublishedAt = request.PublishImmediately ? DateTimeOffset.UtcNow : null,
            ChangeNote = request.ChangeNote
        };

        _dbContext.PromptTemplateVersions.Add(version);

        if (request.PublishImmediately)
        {
            foreach (var v in template.Versions) v.IsPublished = false;
            template.PublishedVersionId = version.Id;
        }

        await _dbContext.SaveChangesAsync(ct);

        return Ok(new { version.Id, version.VersionNumber, version.IsPublished });
    }

    [HttpPut("{id:guid}/versions/{versionId:guid}/publish")]
    public async Task<IActionResult> PublishVersion(Guid id, Guid versionId, CancellationToken ct)
    {
        var template = await _dbContext.PromptTemplates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template == null) return NotFound(new { error = "Template not found" });

        var targetVersion = template.Versions.FirstOrDefault(v => v.Id == versionId);
        if (targetVersion == null) return NotFound(new { error = "Version not found in this template" });

        foreach (var v in template.Versions)
        {
            v.IsPublished = (v.Id == versionId);
        }

        targetVersion.PublishedAt = DateTimeOffset.UtcNow;
        template.PublishedVersionId = targetVersion.Id;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { success = true, publishedVersionId = targetVersion.Id, versionNumber = targetVersion.VersionNumber });
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> TestTemplate(Guid id, [FromBody] TestTemplateRequest request, CancellationToken ct)
    {
        var template = await _dbContext.PromptTemplates
            .Include(t => t.PublishedVersion)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template == null) return NotFound(new { error = "Template not found" });

        var systemPrompt = request.SystemPrompt ?? template.PublishedVersion?.SystemPrompt ?? string.Empty;
        var userTemplate = request.UserPromptTemplate ?? template.PublishedVersion?.UserPromptTemplate ?? string.Empty;

        // Interpolate variables
        var renderedUserPrompt = userTemplate;
        if (request.Variables != null)
        {
            foreach (var (k, v) in request.Variables)
            {
                var valStr = v?.ToString() ?? string.Empty;
                renderedUserPrompt = Regex.Replace(renderedUserPrompt, $"\\{{\\{{\\s*{Regex.Escape(k)}\\s*\\}}\\}}", valStr, RegexOptions.IgnoreCase);
            }
        }

        var chatRequest = new LlmChatRequest
        {
            Model = request.ModelCode,
            Temperature = (double)(request.Temperature ?? 0.7m),
            MaxTokens = request.MaxTokens ?? 1024,
            Messages = new List<ChatMessageDto>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = renderedUserPrompt }
            }
        };

        try
        {
            var response = await _routerService.RouteChatAsync(chatRequest, _gatewayContext, ct);
            return Ok(new
            {
                renderedSystemPrompt = systemPrompt,
                renderedUserPrompt,
                response = response.Content,
                model = response.Model,
                promptTokens = response.PromptTokens,
                completionTokens = response.CompletionTokens,
                totalTokens = response.TotalTokens,
                costUsd = response.CostUsd,
                latencyMs = response.LatencyMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing prompt template.");
            return StatusCode(500, new { error = ex.Message, renderedUserPrompt });
        }
    }
}

public record CreateTemplateRequest(
    Guid? ProjectId,
    string Code,
    string Name,
    string? Description,
    string? SourceSystem,
    string? Module,
    string? VariablesSchemaJson,
    string? SystemPrompt,
    string? UserPromptTemplate
);

public record UpdateTemplateRequest(
    string Name,
    string? Description,
    string? SourceSystem,
    string? Module,
    string? VariablesSchemaJson,
    bool IsActive
);

public record CreateVersionRequest(
    string? SystemPrompt,
    string? UserPromptTemplate,
    string? ChangeNote,
    bool PublishImmediately = false
);

public record TestTemplateRequest(
    string? ModelCode,
    string? SystemPrompt,
    string? UserPromptTemplate,
    decimal? Temperature,
    int? MaxTokens,
    Dictionary<string, object?>? Variables
);
