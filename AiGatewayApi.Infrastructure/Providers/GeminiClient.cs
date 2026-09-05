using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiGatewayApi.Infrastructure.Providers;

public class GeminiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient httpClient, ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LlmChatResponse> ChatCompleteAsync(
        LlmChatRequest request,
        AiProvider provider,
        AiModel model,
        string apiKeyPlaintext,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var modelId = string.IsNullOrWhiteSpace(model.ModelCode) ? "gemini-1.5-flash" : model.ModelCode;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKeyPlaintext.Trim()}";

        var payload = BuildGeminiPayload(request);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Gemini call failed ({Status}): {Error}", response.StatusCode, errBody);
            throw new HttpRequestException($"Gemini returned {response.StatusCode}: {errBody}", null, response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonNode.Parse(json);

        var text = parsed?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? string.Empty;
        var promptTokens = parsed?["usageMetadata"]?["promptTokenCount"]?.GetValue<int>() ?? 0;
        var compTokens = parsed?["usageMetadata"]?["candidatesTokenCount"]?.GetValue<int>() ?? 0;

        var inRate = model.InputPricePer1K / 1000m;
        var outRate = model.OutputPricePer1K / 1000m;
        var cost = (promptTokens * inRate) + (compTokens * outRate);

        return new LlmChatResponse
        {
            Model = model.ModelCode,
            Provider = provider.Name,
            Content = text,
            PromptTokens = promptTokens,
            CompletionTokens = compTokens,
            CostUsd = cost,
            LatencyMs = sw.ElapsedMilliseconds
        };
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        LlmChatRequest request,
        AiProvider provider,
        AiModel model,
        string apiKeyPlaintext,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var modelId = string.IsNullOrWhiteSpace(model.ModelCode) ? "gemini-1.5-flash" : model.ModelCode;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:streamGenerateContent?alt=sse&key={apiKeyPlaintext.Trim()}";

        var payload = BuildGeminiPayload(request);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested && await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..].Trim();
                string? deltaText = null;
                try
                {
                    var chunk = JsonNode.Parse(data);
                    deltaText = chunk?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                }
                catch { }

                if (!string.IsNullOrEmpty(deltaText))
                {
                    yield return deltaText;
                }
            }
        }
    }

    public async Task<LlmEmbeddingResponse> GenerateEmbeddingsAsync(
        LlmEmbeddingRequest request,
        AiProvider provider,
        AiModel model,
        string apiKeyPlaintext,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var modelId = string.IsNullOrWhiteSpace(model.ModelCode) ? "text-embedding-004" : model.ModelCode;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:embedContent?key={apiKeyPlaintext.Trim()}";

        var payload = new
        {
            model = $"models/{modelId}",
            content = new
            {
                parts = request.Input.Select(i => new { text = i }).ToArray()
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonNode.Parse(json);

        var embeddings = new List<float[]>();
        if (parsed?["embedding"]?["values"] is JsonArray values)
        {
            embeddings.Add(values.Select(v => v?.GetValue<float>() ?? 0f).ToArray());
        }

        return new LlmEmbeddingResponse
        {
            Model = model.ModelCode,
            Embeddings = embeddings,
            TotalTokens = request.Input.Sum(s => s.Length / 4),
            CostUsd = 0,
            LatencyMs = sw.ElapsedMilliseconds
        };
    }

    public async Task<bool> PingHealthAsync(
        AiProvider provider,
        string apiKeyPlaintext,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKeyPlaintext.Trim()}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var response = await _httpClient.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static object BuildGeminiPayload(LlmChatRequest request)
    {
        var contents = new List<object>();

        foreach (var m in request.Messages)
        {
            var role = m.Role.ToLowerInvariant() == "assistant" ? "model" : "user";
            var parts = new List<object>();

            if (!string.IsNullOrWhiteSpace(m.Content))
            {
                parts.Add(new { text = m.Content });
            }

            if (!string.IsNullOrWhiteSpace(m.ImageUrl))
            {
                // check if base64 data url
                var img = m.ImageUrl;
                if (img.StartsWith("data:"))
                {
                    var comma = img.IndexOf(',');
                    if (comma > 0)
                    {
                        var header = img[..comma];
                        var mime = header.Replace("data:", "").Replace(";base64", "").Trim();
                        var base64Data = img[(comma + 1)..];
                        parts.Add(new
                        {
                            inline_data = new
                            {
                                mime_type = string.IsNullOrEmpty(mime) ? "image/jpeg" : mime,
                                data = base64Data
                            }
                        });
                    }
                }
            }

            contents.Add(new { role, parts });
        }

        var dict = new Dictionary<string, object>
        {
            ["contents"] = contents,
            ["generationConfig"] = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens ?? 2048
            }
        };

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            dict["systemInstruction"] = new
            {
                parts = new[] { new { text = request.SystemPrompt } }
            };
        }

        return dict;
    }
}
