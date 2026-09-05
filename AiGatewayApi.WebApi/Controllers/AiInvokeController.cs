using System.Text.Json;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiGatewayApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/ai")]
public class AiInvokeController : ControllerBase
{
    private readonly ILlmRouterService _routerService;
    private readonly IGatewayKeyContext _gatewayContext;
    private readonly ILogger<AiInvokeController> _logger;

    public AiInvokeController(
        ILlmRouterService routerService,
        IGatewayKeyContext gatewayContext,
        ILogger<AiInvokeController> logger)
    {
        _routerService = routerService;
        _gatewayContext = gatewayContext;
        _logger = logger;
    }

    [HttpPost("chat/complete")]
    public async Task<IActionResult> ChatComplete([FromBody] LlmChatRequest request, CancellationToken ct)
    {
        if (request.Stream)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            await foreach (var chunk in _routerService.RouteStreamAsync(request, _gatewayContext, ct))
            {
                var payload = $"data: {chunk}\n\n";
                await Response.WriteAsync(payload, ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
            return new EmptyResult();
        }

        try
        {
            var response = await _routerService.RouteChatAsync(request, _gatewayContext, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Routing or quota error during chat completion.");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during chat completion.");
            return StatusCode(500, new { error = "An internal error occurred while processing LLM request." });
        }
    }

    [HttpPost("embeddings")]
    public async Task<IActionResult> Embeddings([FromBody] LlmEmbeddingRequest request, CancellationToken ct)
    {
        if (request.Input == null || request.Input.Count == 0)
        {
            return BadRequest(new { error = "Input text list cannot be empty." });
        }

        try
        {
            var response = await _routerService.RouteEmbeddingAsync(request, _gatewayContext, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embeddings.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("document/ocr")]
    public async Task<IActionResult> DocumentOcr([FromBody] DocumentOcrRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64) && string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return BadRequest(new { error = "Either ImageBase64 or ImageUrl must be provided." });
        }

        try
        {
            var response = await _routerService.ProcessOcrAsync(request, _gatewayContext, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during document OCR.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("content/generate")]
    public async Task<IActionResult> ContentGenerate([FromBody] ContentGenerateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new { error = "Topic must be specified." });
        }

        try
        {
            var generatedText = await _routerService.GenerateContentAsync(request, _gatewayContext, ct);
            return Ok(new { content = generatedText, generatedAt = DateTimeOffset.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("forecast/predict")]
    public async Task<IActionResult> ForecastPredict([FromBody] ForecastPredictRequest request, CancellationToken ct)
    {
        if (request.HistoricalValues == null || request.HistoricalValues.Count == 0)
        {
            return BadRequest(new { error = "Historical values cannot be empty." });
        }

        try
        {
            var response = await _routerService.PredictForecastAsync(request, _gatewayContext, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing forecast prediction.");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
