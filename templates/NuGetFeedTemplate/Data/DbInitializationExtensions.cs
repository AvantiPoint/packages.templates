using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AvantiPoint.Packages.Database.SqlServer;

namespace NuGetFeedTemplate.Data;

public static class DbInitializationExtensions
{
    public static async Task InitializeDatabaseContext(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        logger.LogInformation("Initializing database contexts...");

        using var scope = app.Services.CreateScope();
        using var feedContext = scope.ServiceProvider.GetRequiredService<FeedContext>();
        using var sqlContext = scope.ServiceProvider.GetRequiredService<SqlServerContext>();

        await ApplyMigrations(feedContext, "FeedContext", logger);
        await ApplyMigrations(sqlContext, "SqlServerContext", logger);

        logger.LogInformation("Database initialization complete.");
    }

    private static async Task ApplyMigrations(DbContext context, string contextName, ILogger logger)
    {
        try
        {
            logger.LogInformation("Checking for pending migrations in {ContextName}...", contextName);
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Found {Count} pending migration(s) in {ContextName}:", pendingMigrations.Count, contextName);
                foreach (var migration in pendingMigrations)
                {
                    logger.LogInformation("  - {MigrationName}", migration);
                }
                
                logger.LogInformation("Applying migrations to {ContextName}...", contextName);
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully to {ContextName}.", contextName);
            }
            else
            {
                logger.LogInformation("No pending migrations for {ContextName}.", contextName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying migrations to {ContextName}.", contextName);
            throw;
        }
    }
}