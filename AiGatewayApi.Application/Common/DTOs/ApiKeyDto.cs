namespace AiGatewayApi.Application.Common.DTOs;

public record ApiKeyDto(
    Guid Id,
    Guid ProjectId,
    string ProjectCode,
    string KeyPrefix,
    string Name,
    string? AllowedModels,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>Tr? v? sau khi generate key — ch?a plaintext (ch? 1 l?n).</summary>
public record GeneratedApiKeyResult(
    Guid Id,
    string KeyPrefix,
    string PlaintextKey,   // Hi?n th? 1 l?n, không luu
    string Name,
    DateTimeOffset? ExpiresAt);

public record GenerateApiKeyRequest(
    Guid ProjectId,
    string Name,
    string[]? AllowedModelIds,
    DateTimeOffset? ExpiresAt);

public record RevokeApiKeyRequest(Guid Id);
