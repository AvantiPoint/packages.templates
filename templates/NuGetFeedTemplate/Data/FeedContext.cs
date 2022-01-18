using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Data.Models;

namespace NuGetFeedTemplate.Data
{
    public class FeedContext : DbContext
    {
        public FeedContext(DbContextOptions<FeedContext> options)
            : base(options)
        {
        }

        public DbSet<AuthToken> AuthTokens { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<PackageGroup> PackageGroups { get; set; }

        public DbSet<PackageGroupMember> PackageGroupMembers { get; set; }

        public DbSet<PublishTarget> PublishTargets { get; set; }

        public DbSet<PackageGroupSyndication> Syndications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>()
                .HasKey(x => x.Email);

            modelBuilder.Entity<AuthToken>()
                .HasKey(x => x.Key);

            modelBuilder.Entity<AuthToken>()
                .Property(x => x.Created)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder.Entity<AuthToken>()
                .Property(x => x.Expires)
                .HasDefaultValueSql("DATEADD(year, 1, SYSDATETIMEOFFSET())");

            modelBuilder.Entity<PackageGroup>()
                .HasKey(x => x.Name);

            modelBuilder.Entity<PackageGroupMember>()
                .HasKey(x => new { x.PackageGroupName, x.PackageId });

            modelBuilder.Entity<PublishTarget>()
                .HasKey(x => x.Name);

            modelBuilder.Entity<PublishTarget>()
                .Property(x => x.Timestamp)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder.Entity<PackageGroupSyndication>()
                .HasKey(x => new { x.PackageGroupName, x.PublishTargetName });
        }
    }
}
