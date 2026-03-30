using CapFinLoan.Admin.Application.DTOs;

namespace CapFinLoan.Admin.Application.Interfaces;

public interface IDocumentClient
{
    Task<DocumentVerificationResponseDto?> GetDocumentByIdAsync(Guid documentId);
    Task<DocumentVerificationResponseDto?> VerifyDocumentAsync(Guid documentId, VerifyDocumentRequestDto request);
}