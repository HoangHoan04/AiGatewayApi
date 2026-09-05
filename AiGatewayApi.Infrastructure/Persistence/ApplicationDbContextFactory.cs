using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AiGatewayApi.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=AiGatewayDb;Username=postgres;Password=root;Encoding=UTF8";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        optionsBuilder.ConfigureWarnings(w => w.Ignore(
            RelationalEventId.PendingModelChangesWarning,
            CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
