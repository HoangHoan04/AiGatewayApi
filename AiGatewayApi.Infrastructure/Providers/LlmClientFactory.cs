using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AiGatewayApi.Infrastructure.Providers;

public interface ILlmClientFactory
{
    ILlmClient GetClient(AiProviderType providerType);
}

public class LlmClientFactory : ILlmClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    public LlmClientFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILlmClient GetClient(AiProviderType providerType)
    {
        return providerType switch
        {
            AiProviderType.Gemini => _serviceProvider.GetRequiredService<GeminiClient>(),
            _ => _serviceProvider.GetRequiredService<OpenAiCompatibleClient>()
        };
    }
}
