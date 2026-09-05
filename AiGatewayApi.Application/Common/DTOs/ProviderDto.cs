using AiGatewayApi.Domain.Enums;

namespace AiGatewayApi.Application.Common.DTOs;

public record ProviderDto(
    Guid Id,
    string Name,
    AiProviderType ProviderType,
    string? BaseUrl,
    bool IsActive,
    string? Notes,
    int ModelCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateProviderRequest(
    string Name,
    AiProviderType ProviderType,
    string ApiKeyPlaintext,
    string? BaseUrl,
    string? Notes);

public record UpdateProviderRequest(
    Guid Id,
    string Name,
    AiProviderType ProviderType,
    string? ApiKeyPlaintext,   // null = không thay d?i key
    string? BaseUrl,
    bool IsActive,
    string? Notes);
