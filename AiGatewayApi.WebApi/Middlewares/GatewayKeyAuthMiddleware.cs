using System.Net;
using System.Text.Json;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.WebApi.Middlewares;

public class GatewayKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayKeyAuthMiddleware> _logger;

    public GatewayKeyAuthMiddleware(RequestDelegate next, ILogger<GatewayKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        IApiKeyHashService apiKeyHashService,
        IGatewayKeyContext gatewayContext)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Only enforce API Key on invoke endpoints (/api/v1/ai/...)
        // Admin management endpoints (/api/v1/projects, /api/v1/providers, etc.) use JWT Bearer or internal key
        if (!path.StartsWith("/api/v1/ai/"))
        {
            await _next(context);
            return;
        }

        // Extract key from Authorization: Bearer <key> or X-Api-Key: <key>
        string? rawKey = null;
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrWhiteSpace(authHeader))
        {
            var headerStr = authHeader.ToString().Trim();
            if (headerStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                rawKey = headerStr.Substring("Bearer ".Length).Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(rawKey) && context.Request.Headers.TryGetValue("X-Api-Key", out var xApiKey))
        {
            rawKey = xApiKey.ToString().Trim();
        }

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "Missing API Key. Pass 'Authorization: Bearer sk-gw-...' or 'X-Api-Key'.");
            return;
        }

        // If client passed a JWT (starts with eyJ), skip API key verification and let JWT auth handle it (e.g. Playground in Admin UI)
        if (rawKey.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase))
        {
            // For playground calls with JWT token, populate a default playground context
            var user = context.User;
            if (gatewayContext is GatewayKeyContext gw)
            {
                var defaultProject = await dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive);
                if (defaultProject != null)
                {
                    gw.ProjectId = defaultProject.Id;
                    gw.ProjectCode = defaultProject.Code;
                    gw.DefaultModelId = defaultProject.DefaultModelId;
                }
            }
            await _next(context);
            return;
        }

        if (!rawKey.StartsWith("sk-gw-", StringComparison.OrdinalIgnoreCase))
        {
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "Invalid API Key format. AI Gateway keys must start with 'sk-gw-'.");
            return;
        }

        // Query active keys
        var activeKeys = await dbContext.ApiKeys
            .Include(k => k.Project)
            .Where(k => k.IsActive && k.RevokedAt == null && k.Project.IsActive)
            .ToListAsync(context.RequestAborted);

        Domain.Entities.ApiKey? matchedKey = null;
        foreach (var key in activeKeys)
        {
            if (apiKeyHashService.VerifyKey(rawKey, key.KeyHash))
            {
                matchedKey = key;
                break;
            }
        }

        if (matchedKey == null)
        {
            _logger.LogWarning("Authentication failed for API Key prefix: {Prefix}", rawKey.Length > 15 ? rawKey[..15] : rawKey);
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "Invalid or revoked API Key.");
            return;
        }

        if (matchedKey.ExpiresAt.HasValue && matchedKey.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "API Key has expired.");
            return;
        }

        // Populate Gateway Context
        if (gatewayContext is GatewayKeyContext gwContext)
        {
            gwContext.ProjectId = matchedKey.ProjectId;
            gwContext.ProjectCode = matchedKey.Project.Code;
            gwContext.ApiKeyId = matchedKey.Id;
            gwContext.CompanyId = matchedKey.CompanyId;
            gwContext.DefaultModelId = matchedKey.Project.DefaultModelId;
            gwContext.AllowedModels = !string.IsNullOrWhiteSpace(matchedKey.AllowedModels)
                ? matchedKey.AllowedModels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : new List<string>();
        }

        await _next(context);
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        var payload = new
        {
            error = new
            {
                code = "UNAUTHORIZED",
                message,
                timestamp = DateTimeOffset.UtcNow
            }
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
