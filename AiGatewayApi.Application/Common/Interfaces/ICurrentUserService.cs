namespace AiGatewayApi.Application.Common.Interfaces;

/// <summary>
/// L?y thông tin admin user t? JWT claims (Admin Portal).
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
}
