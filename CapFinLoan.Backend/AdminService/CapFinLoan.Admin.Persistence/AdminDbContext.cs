using CapFinLoan.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Admin.Persistence;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    public DbSet<Decision> Decisions => Set<Decision>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("admin");

        modelBuilder.Entity<Decision>(entity =>
        {
            entity.ToTable("Decisions");
            entity.HasKey(x => x.DecisionId);

            entity.Property(x => x.ApplicationId).IsRequired();
            entity.Property(x => x.AdminUserId).IsRequired();

            entity.Property(x => x.DecisionStatus)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Remarks)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.SanctionAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.InterestRate)
                .HasPrecision(5, 2);

            entity.Property(x => x.DecidedBy)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.DecidedAt).IsRequired();

            entity.HasIndex(x => x.ApplicationId).IsUnique();
            entity.HasIndex(x => x.AdminUserId);
            entity.HasIndex(x => x.DecidedAt);
        });

        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.ToTable("ApplicationStatusHistory");
            entity.HasKey(x => x.HistoryId);

            entity.Property(x => x.ApplicationId).IsRequired();
            entity.Property(x => x.AdminUserId).IsRequired();

            entity.Property(x => x.FromStatus)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ToStatus)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ChangedBy)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Remarks)
                .HasMaxLength(1000);

            entity.Property(x => x.ChangedAt).IsRequired();

            entity.HasIndex(x => x.ApplicationId);
            entity.HasIndex(x => x.AdminUserId);
            entity.HasIndex(x => x.ChangedAt);
        });
    }
}
