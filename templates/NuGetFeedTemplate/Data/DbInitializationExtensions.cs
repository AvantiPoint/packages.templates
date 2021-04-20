using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AvantiPoint.Packages.Database.SqlServer;

namespace NuGetFeedTemplate.Data
{
    public static class DbInitializationExtensions
    {
        public static async Task InitializeDatabaseContext(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            using var feedContext = scope.ServiceProvider.GetRequiredService<FeedContext>();
            using var sqlContext = scope.ServiceProvider.GetRequiredService<SqlServerContext>();

            await ApplyMigrations(feedContext);
            await ApplyMigrations(sqlContext);
        }

        private static async Task ApplyMigrations(DbContext context)
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
                await context.Database.MigrateAsync();
        }
    }
}