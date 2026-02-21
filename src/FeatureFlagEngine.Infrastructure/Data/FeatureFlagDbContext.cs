using FeatureFlagEngine.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureFlagEngine.Infrastructure.Data;

public class FeatureFlagDbContext : DbContext
{
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<UserOverride> UserOverrides => Set<UserOverride>();
    public DbSet<GroupOverride> GroupOverrides => Set<GroupOverride>();
    public DbSet<RegionOverride> RegionOverrides => Set<RegionOverride>();

    public FeatureFlagDbContext(DbContextOptions<FeatureFlagDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.HasKey(f => f.Name);
            entity.Property(f => f.Name).HasMaxLength(256);
            entity.Property(f => f.Description).HasMaxLength(1024);

            entity.HasMany(f => f.UserOverrides)
                  .WithOne()
                  .HasForeignKey(u => u.FeatureFlagName)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(f => f.GroupOverrides)
                  .WithOne()
                  .HasForeignKey(g => g.FeatureFlagName)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(f => f.RegionOverrides)
                  .WithOne()
                  .HasForeignKey(r => r.FeatureFlagName)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserOverride>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => new { u.FeatureFlagName, u.UserId }).IsUnique();
        });

        modelBuilder.Entity<GroupOverride>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.HasIndex(g => new { g.FeatureFlagName, g.GroupId }).IsUnique();
        });

        modelBuilder.Entity<RegionOverride>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.FeatureFlagName, r.RegionId }).IsUnique();
        });
    }
}
