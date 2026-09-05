using System.Text.Json.Serialization;
using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Application.Common.Models;

public class ChatMessageDto
{
    public string Role { get; set; } = "user"; // system, user, assistant
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; } // Optional base64 or URL for multimodal/vision/OCR
}

public class LlmChatRequest
{
    public string? Model { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
    public string? PromptTemplateCode { get; set; }
    public Dictionary<string, object>? Variables { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; } = false;
    public string? JsonSchema { get; set; }
    public string? SystemPrompt { get; set; }
}

public class LlmChatResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Model { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal CostUsd { get; set; }
    public long LatencyMs { get; set; }
    public string FinishReason { get; set; } = "stop";
}

public class LlmEmbeddingRequest
{
    public string? Model { get; set; }
    public List<string> Input { get; set; } = new();
}

public class LlmEmbeddingResponse
{
    public string Model { get; set; } = string.Empty;
    public List<float[]> Embeddings { get; set; } = new();
    public int TotalTokens { get; set; }
    public decimal CostUsd { get; set; }
    public long LatencyMs { get; set; }
}

public class DocumentOcrRequest
{
    public string? ImageBase64 { get; set; }
    public string? ImageUrl { get; set; }
    public string DocumentType { get; set; } = "INVOICE"; // INVOICE, ID_CARD, CONTRACT, GENERAL
    public bool RunAsync { get; set; } = false;
    public string? Model { get; set; }
}

public class DocumentOcrResponse
{
    public string DocumentType { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public object? ExtractedData { get; set; }
    public int TokensUsed { get; set; }
    public decimal CostUsd { get; set; }
    public long LatencyMs { get; set; }
    public string? JobId { get; set; }
    public string Status { get; set; } = "COMPLETED";
}

public class ContentGenerateRequest
{
    public string ContentType { get; set; } = "PRODUCT_DESCRIPTION"; // PRODUCT_DESCRIPTION, EMAIL, ANNOUNCEMENT, REPORT
    public string Topic { get; set; } = string.Empty;
    public string? Tone { get; set; } = "Chuyên nghiệp, tin cậy";
    public string? TargetAudience { get; set; }
    public Dictionary<string, object>? Attributes { get; set; }
    public string? Model { get; set; }
}

public class ForecastPredictRequest
{
    public string MetricName { get; set; } = string.Empty;
    public List<decimal> HistoricalValues { get; set; } = new();
    public List<string>? Timestamps { get; set; }
    public int ForecastPeriods { get; set; } = 3;
    public string? ContextDescription { get; set; }
    public string? Model { get; set; }
}

public class ForecastPredictResponse
{
    public string MetricName { get; set; } = string.Empty;
    public List<decimal> PredictedValues { get; set; } = new();
    public string AnalysisSummary { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = "STABLE"; // UP, DOWN, STABLE
    public decimal ConfidenceScore { get; set; } = 0.85m;
    public int TokensUsed { get; set; }
    public decimal CostUsd { get; set; }
}
