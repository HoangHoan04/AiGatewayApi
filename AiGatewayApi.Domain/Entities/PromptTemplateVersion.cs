using AiGatewayApi.Domain.Common;

namespace AiGatewayApi.Domain.Entities;

public class PromptTemplateVersion : BaseEntity
{
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string? UserPromptTemplate { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public string? ChangeNote { get; set; }

    public PromptTemplate Template { get; set; } = null!;
}
