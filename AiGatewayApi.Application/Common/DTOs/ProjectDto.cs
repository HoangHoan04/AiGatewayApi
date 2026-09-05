namespace AiGatewayApi.Application.Common.DTOs;

public record ProjectDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    Guid? DefaultModelId,
    string? DefaultModelName,
    int ApiKeyCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    Guid? DefaultModelId,
    string? DefaultModelName,
    List<ApiKeyDto> ApiKeys,
    QuotaDto? Quota,
    DateTimeOffset CreatedAt);

public record CreateProjectRequest(
    string Name,
    string Code,
    string? Description,
    Guid? DefaultModelId);

public record UpdateProjectRequest(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    Guid? DefaultModelId);
