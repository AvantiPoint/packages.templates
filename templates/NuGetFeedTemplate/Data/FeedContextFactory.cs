using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NuGetFeedTemplate.Data;

/// <summary>
/// Design-time factory for FeedContext to enable EF Core migrations.
/// This factory is used by EF Core tools (dotnet ef migrations add, etc.) 
/// to create instances of the DbContext at design time.
/// </summary>
public class FeedContextFactory : IDesignTimeDbContextFactory<FeedContext>
{
    public FeedContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Template.json", optional: true)
            .Build();

        // Get the connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in appsettings.json or appsettings.Template.json. " +
                "Please ensure the connection string is configured in one of these files.");
        }

        // Create DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<FeedContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new FeedContext(optionsBuilder.Options);
    }
}
