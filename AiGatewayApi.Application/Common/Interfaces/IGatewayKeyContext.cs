namespace AiGatewayApi.Application.Common.Interfaces;

/// <summary>
/// Context du?c inject b?i GatewayAuthMiddleware sau khi xác th?c internal API key.
/// Ch?a thông tin project và key dã du?c resolve.
/// </summary>
public interface IGatewayKeyContext
{
    Guid ProjectId { get; set; }
    string ProjectCode { get; set; }
    Guid ApiKeyId { get; set; }
    Guid? DefaultModelId { get; set; }
}
