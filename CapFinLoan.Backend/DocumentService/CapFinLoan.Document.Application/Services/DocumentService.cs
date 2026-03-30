using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Domain.Entities;
using CapFinLoan.Document.Domain.Enums;
using DocumentEntity = CapFinLoan.Document.Domain.Entities.Document;

namespace CapFinLoan.Document.Application.Services;

public class DocumentService : IDocumentService
{
    private static readonly DocumentType[] RequiredDocumentTypes =
    [
        DocumentType.IdProof,
        DocumentType.AddressProof,
        DocumentType.BankStatement,
        DocumentType.IncomeProof
    ];

    private const string DocsPendingStatus = "DocsPending";
    private const string DocsVerifiedStatus = "DocsVerified";

    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IApplicationServiceClient _applicationServiceClient;

    public DocumentService(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        IApplicationServiceClient applicationServiceClient)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _applicationServiceClient = applicationServiceClient;
    }

    public async Task<DocumentResponseDto> UploadDocumentAsync(
        Guid userId,
        UploadDocumentDto request,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (request.ApplicationId == Guid.Empty)
        {
            throw new ArgumentException("ApplicationId is required.");
        }

        if (request.File is null)
        {
            throw new ArgumentException("File is required.");
        }

        var parsedDocumentType = ParseDocumentType(request.DocumentType);

        var isValidApplication = await _applicationServiceClient.ValidateApplicationAccessAsync(
            request.ApplicationId,
            userId,
            bearerToken,
            cancellationToken);

        if (!isValidApplication)
        {
            throw new UnauthorizedAccessException("You are not allowed to upload documents for this application.");
        }

        _fileStorageService.ValidateFile(request.File);

        var existing = await _documentRepository.GetByApplicationIdAndTypeAsync(request.ApplicationId, parsedDocumentType);

        if (existing is not null)
        {
            if (existing.UserId != userId)
            {
                throw new UnauthorizedAccessException("You are not allowed to replace this document.");
            }

            await _fileStorageService.DeleteFileIfExistsAsync(existing.FilePath, cancellationToken);
            var (savedFileName, savedFilePath) = await _fileStorageService.SaveFileAsync(request.File, cancellationToken);

            existing.FileName = savedFileName;
            existing.FilePath = savedFilePath;
            existing.Status = DocumentStatus.Pending;
            existing.Remarks = null;
            existing.UploadedAt = DateTime.UtcNow;

            await _documentRepository.UpdateAsync(existing);
            await _documentRepository.SaveChangesAsync();

            await SyncApplicationStatusAsync(existing.ApplicationId, bearerToken, cancellationToken);
            return MapToResponse(existing);
        }

        var (newFileName, newFilePath) = await _fileStorageService.SaveFileAsync(request.File, cancellationToken);

        var document = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            ApplicationId = request.ApplicationId,
            UserId = userId,
            FileName = newFileName,
            FilePath = newFilePath,
            DocumentType = parsedDocumentType,
            Status = DocumentStatus.Pending,
            Remarks = null,
            UploadedAt = DateTime.UtcNow
        };

        await _documentRepository.AddAsync(document);
        await _documentRepository.SaveChangesAsync();

        await SyncApplicationStatusAsync(document.ApplicationId, bearerToken, cancellationToken);
        return MapToResponse(document);
    }

    public async Task<DocumentResponseDto> ReplaceDocumentAsync(
        Guid userId,
        Guid documentId,
        UploadDocumentDto request,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (request.File is null)
        {
            throw new ArgumentException("File is required.");
        }

        var parsedDocumentType = ParseDocumentType(request.DocumentType);

        var existing = await _documentRepository.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException("Document not found.");

        if (existing.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not allowed to replace this document.");
        }

        if (existing.ApplicationId != request.ApplicationId || existing.DocumentType != parsedDocumentType)
        {
            throw new ArgumentException("ApplicationId and DocumentType must match the existing document.");
        }

        var isValidApplication = await _applicationServiceClient.ValidateApplicationAccessAsync(
            request.ApplicationId,
            userId,
            bearerToken,
            cancellationToken);

        if (!isValidApplication)
        {
            throw new UnauthorizedAccessException("You are not allowed to upload documents for this application.");
        }

        _fileStorageService.ValidateFile(request.File);

        await _fileStorageService.DeleteFileIfExistsAsync(existing.FilePath, cancellationToken);
        var (savedFileName, savedFilePath) = await _fileStorageService.SaveFileAsync(request.File, cancellationToken);

        existing.FileName = savedFileName;
        existing.FilePath = savedFilePath;
        existing.Status = DocumentStatus.Pending;
        existing.Remarks = null;
        existing.UploadedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(existing);
        await _documentRepository.SaveChangesAsync();

        await SyncApplicationStatusAsync(existing.ApplicationId, bearerToken, cancellationToken);
        return MapToResponse(existing);
    }

    public async Task<IReadOnlyList<DocumentResponseDto>> GetDocumentsByApplicationIdAsync(
        Guid userId,
        Guid applicationId,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (applicationId == Guid.Empty)
        {
            throw new ArgumentException("ApplicationId is required.");
        }

        var isValidApplication = await _applicationServiceClient.ValidateApplicationAccessAsync(
            applicationId,
            userId,
            bearerToken,
            cancellationToken);

        if (!isValidApplication)
        {
            throw new UnauthorizedAccessException("You are not allowed to access this application.");
        }

        var documents = await _documentRepository.GetByApplicationIdAndUserIdAsync(applicationId, userId);
        return documents.Select(MapToResponse).ToList();
    }

    public async Task<DocumentResponseDto> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException("Document not found.");

        return MapToResponse(document);
    }

    public async Task<DocumentResponseDto> VerifyDocumentAsync(
        Guid adminUserId,
        Guid documentId,
        VerifyDocumentDto request,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Invalid admin user identifier.");
        }

        var parsedStatus = ParseVerificationStatus(request.Status);

        if (parsedStatus is not DocumentStatus.Verified and not DocumentStatus.Rejected)
        {
            throw new ArgumentException("Document status must be either Verified or Rejected.");
        }

        var document = await _documentRepository.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException("Document not found.");

        document.Status = parsedStatus;
        document.Remarks = string.IsNullOrWhiteSpace(request.Remarks)
            ? null
            : request.Remarks.Trim();

        await _documentRepository.UpdateAsync(document);
        await _documentRepository.SaveChangesAsync();

        await SyncApplicationStatusAsync(document.ApplicationId, bearerToken, cancellationToken);
        return MapToResponse(document);
    }

    private async Task SyncApplicationStatusAsync(Guid applicationId, string? bearerToken, CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetByApplicationIdAsync(applicationId);

        var documentsByType = documents
            .GroupBy(x => x.DocumentType)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(d => d.UploadedAt).First());

        var hasAllDocs = RequiredDocumentTypes.All(docType => documentsByType.ContainsKey(docType));
        var anyRejected = documentsByType.Values.Any(x => x.Status == DocumentStatus.Rejected);
        var allVerified = hasAllDocs
            && RequiredDocumentTypes.All(docType => documentsByType[docType].Status == DocumentStatus.Verified);

        var targetStatus = allVerified
            ? DocsVerifiedStatus
            : DocsPendingStatus;

        if (anyRejected)
        {
            targetStatus = DocsPendingStatus;
        }

        if (!hasAllDocs)
        {
            targetStatus = DocsPendingStatus;
        }

        var request = new ApplicationDocumentStatusUpdateDto
        {
            ApplicationId = applicationId,
            Status = targetStatus
        };

        await _applicationServiceClient.UpdateDocumentStatusAsync(request, bearerToken, cancellationToken);
    }

    private static DocumentResponseDto MapToResponse(DocumentEntity document)
    {
        return new DocumentResponseDto
        {
            Id = document.Id,
            ApplicationId = document.ApplicationId,
            UserId = document.UserId,
            FileName = document.FileName,
            FilePath = document.FilePath,
            DocumentType = document.DocumentType.ToString(),
            Status = document.Status.ToString(),
            Remarks = document.Remarks,
            UploadedAt = document.UploadedAt
        };
    }

    private static DocumentType ParseDocumentType(string rawDocumentType)
    {
        if (string.IsNullOrWhiteSpace(rawDocumentType))
        {
            throw new ArgumentException("DocumentType is required. Allowed values: ID_PROOF, ADDRESS_PROOF, BANK_STATEMENT, INCOME_PROOF.");
        }

        if (int.TryParse(rawDocumentType.Trim(), out _))
        {
            throw new ArgumentException("Numeric DocumentType is not allowed. Use literal values: ID_PROOF, ADDRESS_PROOF, BANK_STATEMENT, INCOME_PROOF.");
        }

        var normalized = rawDocumentType.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");
        var canonical = normalized switch
        {
            "IDPROOF" or "ID_PROOF" => nameof(DocumentType.IdProof),
            "ADDRESSPROOF" or "ADDRESS_PROOF" => nameof(DocumentType.AddressProof),
            "BANKSTATEMENT" or "BANK_STATEMENT" => nameof(DocumentType.BankStatement),
            "INCOMEPROOF" or "INCOME_PROOF" => nameof(DocumentType.IncomeProof),
            _ => rawDocumentType.Trim()
        };

        if (!Enum.TryParse<DocumentType>(canonical, true, out var parsedDocumentType))
        {
            throw new ArgumentException("Invalid DocumentType. Allowed values: ID_PROOF, ADDRESS_PROOF, BANK_STATEMENT, INCOME_PROOF.");
        }

        return parsedDocumentType;
    }

    private static DocumentStatus ParseVerificationStatus(string rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            throw new ArgumentException("Status is required. Allowed values: Verified, Rejected.");
        }

        if (int.TryParse(rawStatus.Trim(), out _))
        {
            throw new ArgumentException("Numeric Status is not allowed. Use literal values: Verified or Rejected.");
        }

        if (!Enum.TryParse<DocumentStatus>(rawStatus.Trim(), true, out var parsedStatus))
        {
            throw new ArgumentException("Invalid Status. Allowed values: Verified or Rejected.");
        }

        return parsedStatus;
    }
}
