using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CapFinLoan.Document.Persistence;

public class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    public DocumentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocumentsDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DocumentServiceConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=CapFinLoanDocumentDb;Trusted_Connection=True;";

        optionsBuilder.UseSqlServer(connectionString);
        return new DocumentsDbContext(optionsBuilder.Options);
    }
}
