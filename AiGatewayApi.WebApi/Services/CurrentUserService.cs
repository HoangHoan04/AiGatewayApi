using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AiGatewayApi.Application.Common.Interfaces;

namespace AiGatewayApi.WebApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("id");

            return Guid.TryParse(userIdStr, out var id) ? id : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("name");

    public string? SourceSystem
    {
        get
        {
            var headerVal = _httpContextAccessor.HttpContext?.Request.Headers["X-Source-System"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerVal)) return headerVal;

            return _httpContextAccessor.HttpContext?.User?.FindFirstValue("source_system") ?? "ERP";
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
