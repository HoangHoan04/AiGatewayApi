using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Domain.Entities;
using AiGatewayApi.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AiGatewayApi.Infrastructure.Providers;

public class OpenAiCompatibleClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiCompatibleClient> _logger;

    public OpenAiCompatibleClient(HttpClient httpClient, ILogger<OpenAiCompatibleClient> logger)
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
        var baseUrl = ResolveBaseUrl(provider);
        var endpoint = $"{baseUrl.TrimEnd('/')}/chat/completions";

        var payload = BuildOpenAiPayload(request, model, stream: false);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        AttachAuth(httpRequest, provider, apiKeyPlaintext);

        var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("OpenAI call failed ({Status}): {Error}", response.StatusCode, errBody);
            throw new HttpRequestException($"Provider {provider.Name} returned {response.StatusCode}: {errBody}", null, response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonNode.Parse(json);

        var content = parsed?["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
        var promptTokens = parsed?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0;
        var compTokens = parsed?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0;

        var cost = CalculateCost(model, promptTokens, compTokens);

        return new LlmChatResponse
        {
            Model = model.ModelCode,
            Provider = provider.Name,
            Content = content,
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
        var baseUrl = ResolveBaseUrl(provider);
        var endpoint = $"{baseUrl.TrimEnd('/')}/chat/completions";

        var payload = BuildOpenAiPayload(request, model, stream: true);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        AttachAuth(httpRequest, provider, apiKeyPlaintext);

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
                if (data == "[DONE]") break;

                string? deltaText = null;
                try
                {
                    var chunk = JsonNode.Parse(data);
                    deltaText = chunk?["choices"]?[0]?["delta"]?["content"]?.ToString();
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
        var baseUrl = ResolveBaseUrl(provider);
        var endpoint = $"{baseUrl.TrimEnd('/')}/embeddings";

        var payload = new
        {
            model = model.ModelCode,
            input = request.Input
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        AttachAuth(httpRequest, provider, apiKeyPlaintext);

        var response = await _httpClient.SendAsync(httpRequest, ct);
        sw.Stop();
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonNode.Parse(json);

        var embeddings = new List<float[]>();
        if (parsed?["data"] is JsonArray dataArr)
        {
            foreach (var item in dataArr)
            {
                if (item?["embedding"] is JsonArray embArr)
                {
                    embeddings.Add(embArr.Select(v => v?.GetValue<float>() ?? 0f).ToArray());
                }
            }
        }

        var totalTokens = parsed?["usage"]?["total_tokens"]?.GetValue<int>() ?? 0;
        var cost = CalculateCost(model, totalTokens, 0);

        return new LlmEmbeddingResponse
        {
            Model = model.ModelCode,
            Embeddings = embeddings,
            TotalTokens = totalTokens,
            CostUsd = cost,
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
            var baseUrl = ResolveBaseUrl(provider);
            var endpoint = $"{baseUrl.TrimEnd('/')}/models";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            AttachAuth(request, provider, apiKeyPlaintext);
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

    private static string ResolveBaseUrl(AiProvider provider)
    {
        if (!string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            return provider.BaseUrl;
        }

        return provider.ProviderType switch
        {
            AiProviderType.OpenAI => "https://api.openai.com/v1",
            AiProviderType.DeepSeek => "https://api.deepseek.com/v1",
            AiProviderType.Groq => "https://api.groq.com/openai/v1",
            AiProviderType.OpenRouter => "https://openrouter.ai/api/v1",
            AiProviderType.Ollama => "http://localhost:11434/v1",
            _ => "https://api.openai.com/v1"
        };
    }

    private static void AttachAuth(HttpRequestMessage req, AiProvider provider, string apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        }

        if (provider.HeadersJson != null)
        {
            try
            {
                var extra = JsonSerializer.Deserialize<Dictionary<string, string>>(provider.HeadersJson);
                if (extra != null)
                {
                    foreach (var (k, v) in extra)
                    {
                        req.Headers.TryAddWithoutValidation(k, v);
                    }
                }
            }
            catch { }
        }
    }

    private static object BuildOpenAiPayload(LlmChatRequest request, AiModel model, bool stream)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }

        foreach (var m in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(m.ImageUrl))
            {
                messages.Add(new { role = m.Role, content = m.Content });
            }
            else
            {
                // Multimodal / Vision
                messages.Add(new
                {
                    role = m.Role,
                    content = new object[]
                    {
                        new { type = "text", text = m.Content },
                        new { type = "image_url", image_url = new { url = m.ImageUrl } }
                    }
                });
            }
        }

        var dict = new Dictionary<string, object>
        {
            ["model"] = model.ModelCode,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["stream"] = stream
        };

        if (request.MaxTokens.HasValue && request.MaxTokens.Value > 0)
        {
            dict["max_tokens"] = request.MaxTokens.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.JsonSchema))
        {
            dict["response_format"] = new { type = "json_object" };
        }

        return dict;
    }

    private static decimal CalculateCost(AiModel model, int inputTokens, int outputTokens)
    {
        var inRate = model.InputPricePer1K / 1000m;
        var outRate = model.OutputPricePer1K / 1000m;
        return (inputTokens * inRate) + (outputTokens * outRate);
    }
}
