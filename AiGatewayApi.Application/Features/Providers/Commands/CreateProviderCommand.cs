using AiGatewayApi.Application.Common.DTOs;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Domain.Entities;
using MediatR;

namespace AiGatewayApi.Application.Features.Providers.Commands;

public record CreateProviderCommand(CreateProviderRequest Request) : IRequest<Guid>;

public class CreateProviderHandler(IApplicationDbContext db, IEncryptionService encryption, ICurrentUserService currentUser)
    : IRequestHandler<CreateProviderCommand, Guid>
{
    public async Task<Guid> Handle(CreateProviderCommand cmd, CancellationToken ct)
    {
        var provider = new AiProvider
        {
            Name = cmd.Request.Name,
            ProviderType = cmd.Request.ProviderType,
            ApiKeyEncrypted = encryption.Encrypt(cmd.Request.ApiKeyPlaintext),
            BaseUrl = cmd.Request.BaseUrl,
            Notes = cmd.Request.Notes,
            CreatedBy = currentUser.UserId,
            UpdatedBy = currentUser.UserId
        };
        db.AiProviders.Add(provider);
        await db.SaveChangesAsync(ct);
        return provider.Id;
    }
}
