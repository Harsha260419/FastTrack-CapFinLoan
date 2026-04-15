using CapFinLoan.Document.Application.DTOs;

namespace CapFinLoan.Document.Application.Interfaces;

public interface IApplicationServiceClient
{
    Task<bool> ValidateApplicationAccessAsync(Guid applicationId, Guid userId, string? bearerToken, CancellationToken cancellationToken = default);
    Task UpdateDocumentStatusAsync(
        ApplicationDocumentStatusUpdateDto request,
        string? bearerToken,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
