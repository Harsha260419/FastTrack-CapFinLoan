using CapFinLoan.Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DocumentEntity = CapFinLoan.Document.Domain.Entities.Document;

namespace CapFinLoan.Document.Persistence;

public class DocumentsDbContext : DbContext
{
    public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("docs");

        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("Documents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(x => x.FilePath)
                .IsRequired()
                .HasMaxLength(1024);

            entity.Property(x => x.Remarks)
                .HasMaxLength(500);

            entity.Property(x => x.DocumentType)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Property(x => x.UploadedAt)
                .IsRequired();

            entity.HasIndex(x => new { x.ApplicationId, x.DocumentType })
                .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
