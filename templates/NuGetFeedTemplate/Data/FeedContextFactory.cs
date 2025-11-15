using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NuGetFeedTemplate.Data
{
    public class FeedContextFactory : IDesignTimeDbContextFactory<FeedContext>
    {
        public FeedContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FeedContext>();
            
            // Use SQL Server for design-time migrations
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=NuGetFeed;Trusted_Connection=True;");
            
            return new FeedContext(optionsBuilder.Options);
        }
    }
}
