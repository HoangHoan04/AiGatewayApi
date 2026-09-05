using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiGatewayApi.Infrastructure.Persistence;

public static class AiGatewayDatabaseBootstrap
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Migrating AiGateway database...");
            await context.Database.MigrateAsync();
            logger.LogInformation("AiGateway database migration completed. No seed data applied (manual creation mode).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize AiGateway database.");
            throw;
        }
    }
}
