using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Domain.Entities;
using CapFinLoan.Document.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using DocumentEntity = CapFinLoan.Document.Domain.Entities.Document;

namespace CapFinLoan.Document.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentsDbContext _dbContext;

    public DocumentRepository(DocumentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentEntity?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DocumentEntity?> GetByApplicationIdAndTypeAsync(Guid applicationId, DocumentType documentType)
    {
        return await _dbContext.Documents
            .FirstOrDefaultAsync(x => x.ApplicationId == applicationId && x.DocumentType == documentType);
    }

    public async Task<List<DocumentEntity>> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _dbContext.Documents
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<DocumentEntity>> GetByApplicationIdAndUserIdAsync(Guid applicationId, Guid userId)
    {
        return await _dbContext.Documents
            .Where(x => x.ApplicationId == applicationId && x.UserId == userId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentEntity document)
    {
        await _dbContext.Documents.AddAsync(document);
    }

    public Task UpdateAsync(DocumentEntity document)
    {
        _dbContext.Documents.Update(document);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
