using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public DbSet<PackageDownload> Downloads { get; set; }

        public DbSet<User> Users { get; set; }

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
        }
    }
}
