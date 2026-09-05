using AiGatewayApi.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.Application.Features.Providers.Commands;

public record DeleteProviderCommand(Guid Id) : IRequest;

public class DeleteProviderHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeleteProviderCommand>
{
    public async Task Handle(DeleteProviderCommand cmd, CancellationToken ct)
    {
        var provider = await db.AiProviders
            .Where(p => p.Id == cmd.Id && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Provider {cmd.Id} not found");

        provider.IsDeleted = true;
        provider.UpdatedBy = currentUser.UserId;
        provider.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
