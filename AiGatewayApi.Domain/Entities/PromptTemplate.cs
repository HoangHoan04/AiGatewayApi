using AiGatewayApi.Domain.Common;

namespace AiGatewayApi.Domain.Entities;

public class PromptTemplate : BaseEntity
{
    public Guid? ProjectId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SourceSystem { get; set; }
    public string? Module { get; set; }
    public string? VariablesSchemaJson { get; set; }
    public Guid? PublishedVersionId { get; set; }
    public bool IsActive { get; set; } = true;

    public Project? Project { get; set; }
    public PromptTemplateVersion? PublishedVersion { get; set; }
    public ICollection<PromptTemplateVersion> Versions { get; set; } = new List<PromptTemplateVersion>();
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
}
