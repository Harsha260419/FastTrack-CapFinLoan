using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CapFinLoan.Document.Persistence;

public class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    public DocumentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocumentsDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CapFinLoanDb;Trusted_Connection=True;");
        return new DocumentsDbContext(optionsBuilder.Options);
    }
}
