namespace AiGatewayApi.Application.Common.DTOs;

public record ModelDto(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    string ModelCode,
    string DisplayName,
    decimal InputPricePer1K,
    decimal OutputPricePer1K,
    int MaxContextTokens,
    bool SupportsStreaming,
    bool IsActive);

public record CreateModelRequest(
    Guid ProviderId,
    string ModelCode,
    string DisplayName,
    decimal InputPricePer1K,
    decimal OutputPricePer1K,
    int MaxContextTokens,
    bool SupportsStreaming);

public record UpdateModelRequest(
    Guid Id,
    string DisplayName,
    decimal InputPricePer1K,
    decimal OutputPricePer1K,
    int MaxContextTokens,
    bool SupportsStreaming,
    bool IsActive);
