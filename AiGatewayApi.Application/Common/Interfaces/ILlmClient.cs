using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Domain.Entities;

namespace AiGatewayApi.Application.Common.Interfaces;

public interface ILlmClient
{
    Task<LlmChatResponse> ChatCompleteAsync(
        LlmChatRequest request,
        AiProvider provider,
        AiModel model,
        string apiKeyPlaintext,
        CancellationToken ct = default);

    IAsyncEnumerable<string> ChatStreamAsync(
        LlmChatRequest request,
        AiProvider provider,
        AiModel model,
        string apiKeyPlaintext,
        CancellationToken ct = default);

    Task<LlmEmbeddingResponse> GenerateEmbeddingsAsync(
        LlmEmbeddingRequest request,
        AiProvider provider,
        AiModel model,
        string apiKeyPlaintext,
        CancellationToken ct = default);

    Task<bool> PingHealthAsync(
        AiProvider provider,
        string apiKeyPlaintext,
        CancellationToken ct = default);
}
