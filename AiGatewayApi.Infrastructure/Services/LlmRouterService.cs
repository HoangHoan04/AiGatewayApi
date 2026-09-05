using System.Diagnostics;
using System.Runtime.CompilerServices;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Domain.Entities;
using AiGatewayApi.Domain.Enums;
using AiGatewayApi.Infrastructure.Persistence;
using AiGatewayApi.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiGatewayApi.Infrastructure.Services;

public interface ILlmRouterService
{
    Task<LlmChatResponse> RouteChatAsync(
        LlmChatRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default);

    IAsyncEnumerable<string> RouteStreamAsync(
        LlmChatRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default);

    Task<LlmEmbeddingResponse> RouteEmbeddingAsync(
        LlmEmbeddingRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default);

    Task<DocumentOcrResponse> ProcessOcrAsync(
        DocumentOcrRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default);

    Task<string> GenerateContentAsync(
        ContentGenerateRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default);

    Task<ForecastPredictResponse> PredictForecastAsync(
        ForecastPredictRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default);
}

public class LlmRouterService : ILlmRouterService
{
    private readonly ApplicationDbContext _db;
    private readonly ILlmClientFactory _clientFactory;
    private readonly IEncryptionService _encryption;
    private readonly IUsageTracker _usageTracker;
    private readonly ILogger<LlmRouterService> _logger;

    public LlmRouterService(
        ApplicationDbContext db,
        ILlmClientFactory clientFactory,
        IEncryptionService encryption,
        IUsageTracker usageTracker,
        ILogger<LlmRouterService> logger)
    {
        _db = db;
        _clientFactory = clientFactory;
        _encryption = encryption;
        _usageTracker = usageTracker;
        _logger = logger;
    }

    public async Task<LlmChatResponse> RouteChatAsync(
        LlmChatRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default)
    {
        await CheckQuotaAsync(context.ProjectId, ct);
        var (model, provider) = await ResolveModelAndProviderAsync(request.Model, context, ct);

        var keyPlaintext = _encryption.Decrypt(provider.ApiKeyEncrypted);
        var client = _clientFactory.GetClient(provider.ProviderType);

        var sw = Stopwatch.StartNew();
        try
        {
            var res = await client.ChatCompleteAsync(request, provider, model, keyPlaintext, ct);
            sw.Stop();

            await _usageTracker.TrackAsync(new UsageRecord(
                context.ProjectId,
                context.ApiKeyId,
                model.Id,
                provider.ProviderType,
                res.PromptTokens,
                res.CompletionTokens,
                (int)sw.ElapsedMilliseconds,
                UsageStatus.Success,
                IsStreaming: false
            ), ct);

            return res;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Primary provider {Provider} failed, attempting fallback...", provider.Name);

            // Try fallback provider if available
            var fallback = await _db.AiProviders
                .Where(p => p.Id != provider.Id && p.IsActive)
                .OrderBy(p => p.Name)
                .FirstOrDefaultAsync(ct);

            if (fallback != null)
            {
                try
                {
                    var fallbackKey = _encryption.Decrypt(fallback.ApiKeyEncrypted);
                    var fallbackClient = _clientFactory.GetClient(fallback.ProviderType);
                    var fallbackRes = await fallbackClient.ChatCompleteAsync(request, fallback, model, fallbackKey, ct);

                    await _usageTracker.TrackAsync(new UsageRecord(
                        context.ProjectId,
                        context.ApiKeyId,
                        model.Id,
                        fallback.ProviderType,
                        fallbackRes.PromptTokens,
                        fallbackRes.CompletionTokens,
                        (int)sw.ElapsedMilliseconds,
                        UsageStatus.Success,
                        IsStreaming: false
                    ), ct);

                    return fallbackRes;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback provider {Fallback} also failed.", fallback.Name);
                }
            }

            await _usageTracker.TrackAsync(new UsageRecord(
                context.ProjectId,
                context.ApiKeyId,
                model.Id,
                provider.ProviderType,
                0,
                0,
                (int)sw.ElapsedMilliseconds,
                UsageStatus.Failed,
                IsStreaming: false,
                ErrorCode: ex.GetType().Name
            ), ct);

            throw;
        }
    }

    public async IAsyncEnumerable<string> RouteStreamAsync(
        LlmChatRequest request,
        IGatewayKeyContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await CheckQuotaAsync(context.ProjectId, ct);
        var (model, provider) = await ResolveModelAndProviderAsync(request.Model, context, ct);

        var keyPlaintext = _encryption.Decrypt(provider.ApiKeyEncrypted);
        var client = _clientFactory.GetClient(provider.ProviderType);

        var sw = Stopwatch.StartNew();
        int chunkCount = 0;

        await foreach (var chunk in client.ChatStreamAsync(request, provider, model, keyPlaintext, ct))
        {
            chunkCount++;
            yield return chunk;
        }
        sw.Stop();

        await _usageTracker.TrackAsync(new UsageRecord(
            context.ProjectId,
            context.ApiKeyId,
            model.Id,
            provider.ProviderType,
            100, // estimated streaming tokens
            chunkCount * 2,
            (int)sw.ElapsedMilliseconds,
            UsageStatus.Success,
            IsStreaming: true
        ), ct);
    }

    public async Task<LlmEmbeddingResponse> RouteEmbeddingAsync(
        LlmEmbeddingRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default)
    {
        await CheckQuotaAsync(context.ProjectId, ct);
        var model = await _db.AiModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.ModelCode == (request.Model ?? "text-embedding-3-small") || m.Capabilities.HasFlag(AiModelCapability.Embedding), ct)
            ?? throw new KeyNotFoundException("No active embedding model found.");

        var provider = model.Provider ?? throw new InvalidOperationException("Provider missing for embedding model.");
        var keyPlaintext = _encryption.Decrypt(provider.ApiKeyEncrypted);
        var client = _clientFactory.GetClient(provider.ProviderType);

        var res = await client.GenerateEmbeddingsAsync(request, provider, model, keyPlaintext, ct);

        await _usageTracker.TrackAsync(new UsageRecord(
            context.ProjectId,
            context.ApiKeyId,
            model.Id,
            provider.ProviderType,
            res.TotalTokens,
            0,
            (int)res.LatencyMs,
            UsageStatus.Success,
            IsStreaming: false
        ), ct);

        return res;
    }

    public async Task<DocumentOcrResponse> ProcessOcrAsync(
        DocumentOcrRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default)
    {
        var prompt = request.DocumentType.ToUpperInvariant() switch
        {
            "INVOICE" => "Bạn là chuyên gia kế toán & OCR hóa đơn. Hãy trích xuất toàn bộ thông tin từ hóa đơn sau sang định dạng JSON thuần túy gồm các trường: invoiceNumber, invoiceDate, sellerName, sellerTaxCode, buyerName, buyerTaxCode, totalBeforeTax, vatRate, vatAmount, totalAmount, items (mảng các sản phẩm có: name, unit, quantity, unitPrice, amount). Trả về JSON duy nhất, không markdown.",
            "ID_CARD" => "Bạn là chuyên gia trích xuất thông tin CCCD / CMND Việt Nam. Hãy đọc và trả về JSON thuần túy gồm: idNumber, fullName, dob, gender, nationality, placeOfOrigin, placeOfResidence, issueDate, expiryDate. Trả về JSON duy nhất, không markdown.",
            _ => "Hãy trích xuất toàn bộ văn bản và dữ liệu có cấu trúc từ tài liệu sau sang JSON rõ ràng."
        };

        var chatReq = new LlmChatRequest
        {
            Model = request.Model,
            SystemPrompt = "Bạn là hệ thống AI OCR doanh nghiệp cực kỳ chính xác. Chỉ trả về JSON thuần.",
            Messages = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Role = "user",
                    Content = prompt,
                    ImageUrl = request.ImageBase64 ?? request.ImageUrl
                }
            },
            Temperature = 0.1,
            JsonSchema = "{}"
        };

        var chatRes = await RouteChatAsync(chatReq, context, ct);

        object? extracted = null;
        try
        {
            var cleaned = chatRes.Content.Trim();
            if (cleaned.StartsWith("```json")) cleaned = cleaned["```json".Length..];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..^3];
            extracted = System.Text.Json.JsonSerializer.Deserialize<object>(cleaned.Trim());
        }
        catch
        {
            extracted = new { raw = chatRes.Content };
        }

        return new DocumentOcrResponse
        {
            DocumentType = request.DocumentType,
            RawText = chatRes.Content,
            ExtractedData = extracted,
            TokensUsed = chatRes.TotalTokens,
            CostUsd = chatRes.CostUsd,
            LatencyMs = chatRes.LatencyMs,
            Status = "COMPLETED"
        };
    }

    public async Task<string> GenerateContentAsync(
        ContentGenerateRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default)
    {
        var prompt = $"Hãy soạn thảo nội dung theo yêu cầu sau:\n" +
                     $"- Loại nội dung: {request.ContentType}\n" +
                     $"- Chủ đề: {request.Topic}\n" +
                     $"- Giọng điệu: {request.Tone ?? "Chuyên nghiệp"}\n" +
                     $"- Đối tượng độc giả: {request.TargetAudience ?? "Khách hàng doanh nghiệp"}\n";

        if (request.Attributes != null)
        {
            prompt += "- Thông tin chi tiết kèm theo:\n";
            foreach (var (k, v) in request.Attributes)
            {
                prompt += $"  + {k}: {v}\n";
            }
        }

        var chatReq = new LlmChatRequest
        {
            Model = request.Model,
            SystemPrompt = "Bạn là trợ lý AI chuyên nghiệp hỗ trợ soạn thảo văn bản, email, mô tả sản phẩm và truyền thông cho doanh nghiệp.",
            Messages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Role = "user", Content = prompt }
            },
            Temperature = 0.7
        };

        var chatRes = await RouteChatAsync(chatReq, context, ct);
        return chatRes.Content;
    }

    public async Task<ForecastPredictResponse> PredictForecastAsync(
        ForecastPredictRequest request,
        IGatewayKeyContext context,
        CancellationToken ct = default)
    {
        var valuesStr = string.Join(", ", request.HistoricalValues);
        var prompt = $"Dưới đây là chuỗi dữ liệu số lịch sử của chỉ số: '{request.MetricName}': [{valuesStr}].\n" +
                     $"Bối cảnh bổ sung: {request.ContextDescription ?? "Hoạt động sản xuất kinh doanh doanh nghiệp"}.\n" +
                     $"Hãy dự báo tiếp {request.ForecastPeriods} kỳ tiếp theo dựa trên xu hướng thực tế. Trả về đúng định dạng JSON thuần với các trường:\n" +
                     $"predictedValues: [danh sách các số dự báo],\n" +
                     $"analysisSummary: \"tóm tắt phân tích ngắn gọn lý do xu hướng\",\n" +
                     $"trendDirection: \"UP\" | \"DOWN\" | \"STABLE\",\n" +
                     $"confidenceScore: số thực từ 0.5 đến 0.99.";

        var chatReq = new LlmChatRequest
        {
            Model = request.Model,
            SystemPrompt = "Bạn là chuyên gia phân tích dữ liệu và dự báo chuỗi thời gian doanh nghiệp. Chỉ trả về JSON.",
            Messages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Role = "user", Content = prompt }
            },
            Temperature = 0.2,
            JsonSchema = "{}"
        };

        var chatRes = await RouteChatAsync(chatReq, context, ct);

        try
        {
            var cleaned = chatRes.Content.Trim();
            if (cleaned.StartsWith("```json")) cleaned = cleaned["```json".Length..];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..^3];
            var parsed = System.Text.Json.JsonSerializer.Deserialize<ForecastPredictResponse>(cleaned.Trim(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed != null)
            {
                parsed.TokensUsed = chatRes.TotalTokens;
                parsed.CostUsd = chatRes.CostUsd;
                return parsed;
            }
        }
        catch { }

        return new ForecastPredictResponse
        {
            MetricName = request.MetricName,
            PredictedValues = request.HistoricalValues.TakeLast(request.ForecastPeriods).ToList(),
            AnalysisSummary = chatRes.Content,
            TrendDirection = "STABLE",
            ConfidenceScore = 0.8m,
            TokensUsed = chatRes.TotalTokens,
            CostUsd = chatRes.CostUsd
        };
    }

    private async Task CheckQuotaAsync(Guid projectId, CancellationToken ct)
    {
        var quota = await _db.Quotas.AsNoTracking().FirstOrDefaultAsync(q => q.ProjectId == projectId, ct);
        if (quota == null) return;

        if (quota.TokenLimit.HasValue && quota.TokenLimit.Value > 0 && quota.CurrentMonthTokens >= quota.TokenLimit.Value)
        {
            throw new InvalidOperationException($"Token limit reached: {quota.CurrentMonthTokens:N0}/{quota.TokenLimit.Value:N0}.");
        }

        if (quota.RequestLimit.HasValue && quota.RequestLimit.Value > 0 && quota.CurrentMonthRequests >= quota.RequestLimit.Value)
        {
            throw new InvalidOperationException($"Request limit reached: {quota.CurrentMonthRequests:N0}/{quota.RequestLimit.Value:N0}.");
        }
    }

    private async Task<(AiModel Model, AiProvider Provider)> ResolveModelAndProviderAsync(
        string? requestedModel,
        IGatewayKeyContext context,
        CancellationToken ct)
    {
        AiModel? model = null;
        if (!string.IsNullOrWhiteSpace(requestedModel))
        {
            model = await _db.AiModels.Include(m => m.Provider).FirstOrDefaultAsync(m => m.ModelCode == requestedModel.Trim() && m.IsActive, ct);
        }

        if (model == null && context.DefaultModelId.HasValue)
        {
            model = await _db.AiModels.Include(m => m.Provider).FirstOrDefaultAsync(m => m.Id == context.DefaultModelId.Value && m.IsActive, ct);
        }

        if (model == null)
        {
            model = await _db.AiModels.Include(m => m.Provider).FirstOrDefaultAsync(m => m.IsActive, ct);
        }

        if (model == null)
        {
            throw new KeyNotFoundException("Không tìm thấy mô hình AI nào đang hoạt động trong hệ thống.");
        }

        var provider = model.Provider ?? await _db.AiProviders.FirstOrDefaultAsync(p => p.Id == model.ProviderId && p.IsActive, ct);
        if (provider == null || !provider.IsActive)
        {
            throw new InvalidOperationException($"Nhà cung cấp cho mô hình '{model.DisplayName}' không khả dụng hoặc bị vô hiệu hóa.");
        }

        return (model, provider);
    }
}
