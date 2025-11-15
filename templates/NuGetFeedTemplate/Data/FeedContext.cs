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

        public DbSet<VulnerabilityRecord> Vulnerabilities { get; set; }

        public DbSet<PackageVulnerabilityRecord> PackageVulnerabilities { get; set; }

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

            // Vulnerability configuration
            modelBuilder.Entity<VulnerabilityRecord>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<VulnerabilityRecord>()
                .Property(x => x.CreatedUtc)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder.Entity<VulnerabilityRecord>()
                .Property(x => x.UpdatedUtc)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            modelBuilder.Entity<VulnerabilityRecord>()
                .HasIndex(x => x.ExternalId)
                .IsUnique();

            modelBuilder.Entity<VulnerabilityRecord>()
                .Property(x => x.ExternalId)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<VulnerabilityRecord>()
                .Property(x => x.Severity)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<VulnerabilityRecord>()
                .Property(x => x.AdvisoryUrl)
                .HasMaxLength(1000);

            // Package Vulnerability configuration
            modelBuilder.Entity<PackageVulnerabilityRecord>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<PackageVulnerabilityRecord>()
                .Property(x => x.PackageId)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<PackageVulnerabilityRecord>()
                .Property(x => x.VersionRange)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<PackageVulnerabilityRecord>()
                .HasIndex(x => x.PackageId);

            modelBuilder.Entity<PackageVulnerabilityRecord>()
                .HasIndex(x => new { x.PackageId, x.VersionRange });

            modelBuilder.Entity<PackageVulnerabilityRecord>()
                .HasOne(x => x.Vulnerability)
                .WithMany(x => x.PackageVulnerabilities)
                .HasForeignKey(x => x.VulnerabilityId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
