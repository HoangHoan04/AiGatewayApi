using AiGatewayApi.Application.Common.Interfaces;

namespace AiGatewayApi.Application.Common.Models;

public class GatewayKeyContext : IGatewayKeyContext
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public Guid ApiKeyId { get; set; }
    public Guid? DefaultModelId { get; set; }
    public Guid? CompanyId { get; set; }
    public List<string> AllowedModels { get; set; } = new();
    public List<string> Scopes { get; set; } = new();
}
