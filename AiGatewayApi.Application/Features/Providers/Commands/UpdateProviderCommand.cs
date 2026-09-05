using AiGatewayApi.Application.Common.DTOs;
using AiGatewayApi.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayApi.Application.Features.Providers.Commands;

public record UpdateProviderCommand(UpdateProviderRequest Request) : IRequest;

public class UpdateProviderHandler(IApplicationDbContext db, IEncryptionService encryption, ICurrentUserService currentUser)
    : IRequestHandler<UpdateProviderCommand>
{
    public async Task Handle(UpdateProviderCommand cmd, CancellationToken ct)
    {
        var provider = await db.AiProviders
            .Where(p => p.Id == cmd.Request.Id && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Provider {cmd.Request.Id} not found");

        provider.Name = cmd.Request.Name;
        provider.ProviderType = cmd.Request.ProviderType;
        provider.BaseUrl = cmd.Request.BaseUrl;
        provider.IsActive = cmd.Request.IsActive;
        provider.Notes = cmd.Request.Notes;
        provider.UpdatedBy = currentUser.UserId;
        provider.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(cmd.Request.ApiKeyPlaintext))
            provider.ApiKeyEncrypted = encryption.Encrypt(cmd.Request.ApiKeyPlaintext);

        await db.SaveChangesAsync(ct);
    }
}
