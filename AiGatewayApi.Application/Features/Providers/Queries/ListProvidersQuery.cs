using AiGatewayApi.Application.Common.DTOs;
using AiGatewayApi.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.Application.Features.Providers.Queries;

public record ListProvidersQuery(bool? IsActive = null) : IRequest<List<ProviderDto>>;

public class ListProvidersHandler(IApplicationDbContext db)
    : IRequestHandler<ListProvidersQuery, List<ProviderDto>>
{
    public async Task<List<ProviderDto>> Handle(ListProvidersQuery req, CancellationToken ct)
    {
        var query = db.AiProviders.Include(p => p.Models).AsNoTracking();
        if (req.IsActive.HasValue)
            query = query.Where(p => p.IsActive == req.IsActive.Value);

        return await query
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new ProviderDto(
                p.Id, p.Name, p.ProviderType, p.BaseUrl,
                p.IsActive, p.Notes,
                p.Models.Count(m => !m.IsDeleted && m.IsActive),
                p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);
    }
}
