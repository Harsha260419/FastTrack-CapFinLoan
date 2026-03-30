using CapFinLoan.Document.Domain.Entities;
using CapFinLoan.Document.Domain.Enums;
using DocumentEntity = CapFinLoan.Document.Domain.Entities.Document;

namespace CapFinLoan.Document.Application.Interfaces;

public interface IDocumentRepository
{
    Task<DocumentEntity?> GetByIdAsync(Guid id);
    Task<DocumentEntity?> GetByApplicationIdAndTypeAsync(Guid applicationId, DocumentType documentType);
    Task<List<DocumentEntity>> GetByApplicationIdAsync(Guid applicationId);
    Task<List<DocumentEntity>> GetByApplicationIdAndUserIdAsync(Guid applicationId, Guid userId);
    Task AddAsync(DocumentEntity document);
    Task UpdateAsync(DocumentEntity document);
    Task SaveChangesAsync();
}
