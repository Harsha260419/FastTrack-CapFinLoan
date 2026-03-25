using CapFinLoan.Application.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Application.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("core");

        modelBuilder.Entity<LoanApplication>(entity =>
        {
            entity.ToTable("LoanApplications");
            entity.HasKey(x => x.ApplicationId);

            entity.Property(x => x.FirstName)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.LastName)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.Gender)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.AddressLine1)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.AddressLine2)
                .HasMaxLength(200);

            entity.Property(x => x.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.State)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.PostalCode)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.EmployerName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.EmploymentType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.MonthlyIncome)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.AnnualIncome)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.ExistingEmiAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.LoanPurpose)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(x => x.LoanAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.TenureMonths)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => x.Status);
        });
    }
}
