using CapFinLoan.Document.Application.DTOs;

namespace CapFinLoan.Document.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponseDto> UploadDocumentAsync(Guid userId, UploadDocumentDto request, string? bearerToken, CancellationToken cancellationToken = default);
    Task<DocumentResponseDto> ReplaceDocumentAsync(Guid userId, Guid documentId, UploadDocumentDto request, string? bearerToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentResponseDto>> GetDocumentsByApplicationIdAsync(Guid userId, Guid applicationId, string? bearerToken, CancellationToken cancellationToken = default);
    Task<DocumentResponseDto> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<DocumentResponseDto> VerifyDocumentAsync(Guid adminUserId, Guid documentId, VerifyDocumentDto request, string? bearerToken, CancellationToken cancellationToken = default);
}
