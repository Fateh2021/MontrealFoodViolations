using Microsoft.EntityFrameworkCore;
using MontrealFoodViolations.Domain.Entities;

namespace MontrealFoodViolations.Infrastructure.Data;

public class MontrealFoodViolationsDbContext : DbContext
{
    public MontrealFoodViolationsDbContext(DbContextOptions<MontrealFoodViolationsDbContext> options) : base(options)
    {
    }

    public DbSet<Violation> Violations => Set<Violation>();
    public DbSet<DatasetSyncState> DatasetSyncStates => Set<DatasetSyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Violation>(entity =>
        {
            entity.HasKey(x => x.IdPoursuite);
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.Date).HasConversion<string?>(
                v => v.HasValue ? v.Value.ToString("yyyyMMdd") : null,
                v => string.IsNullOrWhiteSpace(v) ? null : DateOnly.ParseExact(v, "yyyyMMdd"));
            entity.Property(x => x.DateJugement).HasConversion<string?>(
                v => v.HasValue ? v.Value.ToString("yyyyMMdd") : null,
                v => string.IsNullOrWhiteSpace(v) ? null : DateOnly.ParseExact(v, "yyyyMMdd"));
            entity.Property(x => x.DateStatut).HasConversion<string?>(
                v => v.HasValue ? v.Value.ToString("yyyyMMdd") : null,
                v => string.IsNullOrWhiteSpace(v) ? null : DateOnly.ParseExact(v, "yyyyMMdd"));

            entity.HasIndex(x => x.BusinessId);
            entity.HasIndex(x => x.Date);
            entity.HasIndex(x => x.Etablissement);
            entity.HasIndex(x => x.Statut);
            entity.HasIndex(x => x.Categorie);
            entity.HasIndex(x => x.Ville);
            entity.Property(x => x.Source).HasMaxLength(40).IsRequired().HasDefaultValue("Montreal");
            entity.HasIndex(x => x.Source);
        });

        modelBuilder.Entity<DatasetSyncState>(entity =>
        {
            entity.HasIndex(x => x.DatasetName).IsUnique();
            entity.Property(x => x.DatasetName).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(50);
        });

        base.OnModelCreating(modelBuilder);
    }
}
