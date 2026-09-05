using AiGatewayApi.Domain.Common;

namespace AiGatewayApi.Domain.Entities;

public class ContentPolicy : BaseEntity
{
    public Guid? ProjectId { get; set; }
    public string Name { get; set; } = "Default";
    public bool BlockSecrets { get; set; } = true;
    public int MaxPromptChars { get; set; } = 100_000;
    public bool StorePrompts { get; set; }
    public int PromptRetentionDays { get; set; }
    public string? BlockedPatternsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public Project? Project { get; set; }
}
