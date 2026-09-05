using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Application.Common.Models;
using AiGatewayApi.Infrastructure.Persistence;
using AiGatewayApi.Infrastructure.Providers;
using AiGatewayApi.Infrastructure.Security;
using AiGatewayApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiGatewayApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b =>
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.ConfigureWarnings(w => w.Ignore(
                RelationalEventId.PendingModelChangesWarning,
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddScoped<IApiKeyHashService, ApiKeyHashService>();
        services.AddScoped<IGatewayKeyContext, GatewayKeyContext>();

        services.AddHttpClient();
        services.AddScoped<OpenAiCompatibleClient>();
        services.AddScoped<GeminiClient>();
        services.AddScoped<ILlmClientFactory, LlmClientFactory>();
        services.AddScoped<IUsageTracker, UsageTracker>();
        services.AddScoped<ILlmRouterService, LlmRouterService>();

        return services;
    }
}
