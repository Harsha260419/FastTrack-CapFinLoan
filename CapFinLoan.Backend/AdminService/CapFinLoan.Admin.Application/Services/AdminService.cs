using CapFinLoan.Admin.Application.DTOs;
using CapFinLoan.Admin.Application.Interfaces;
using CapFinLoan.Admin.Application.Constants;
using CapFinLoan.Admin.Domain.Entities;
using CapFinLoan.Messaging.Contracts;
using MassTransit;

namespace CapFinLoan.Admin.Application.Services;

public class AdminService : IAdminService
{
    private readonly IApplicationClient _applicationClient;
    private readonly IDocumentClient _documentClient;
    private readonly IDecisionRepository _decisionRepository;
    private readonly IStatusHistoryRepository _statusHistoryRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public AdminService(
        IApplicationClient applicationClient,
        IDocumentClient documentClient,
        IDecisionRepository decisionRepository,
        IStatusHistoryRepository statusHistoryRepository,
        IPublishEndpoint publishEndpoint)
    {
        _applicationClient = applicationClient;
        _documentClient = documentClient;
        _decisionRepository = decisionRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<IReadOnlyList<AdminApplicationQueueItemDto>> GetApplicationsAsync()
    {
        var applications = await _applicationClient.GetApplicationsAsync(null);

        return applications.Select(a => new AdminApplicationQueueItemDto
        {
            ApplicationId = a.ApplicationId,
            ApplicantName = a.FullName,
            LoanAmount = a.LoanAmount,
            Status = AdminStatusConstants.Normalize(a.Status),
            CreatedDate = a.CreatedAt
        }).ToList();
    }

    public async Task<AdminApplicationDetailsDto> GetApplicationByIdAsync(Guid applicationId)
    {
        var application = await _applicationClient.GetApplicationByIdAsync(applicationId);
        if (application is null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        return new AdminApplicationDetailsDto
        {
            ApplicationId = application.ApplicationId,
            ApplicantName = application.FullName,
            Email = application.Email,
            PhoneNumber = application.PhoneNumber,
            LoanAmount = application.LoanAmount,
            LoanPurpose = application.LoanPurpose,
            TenureMonths = application.TenureMonths,
            CurrentStatus = AdminStatusConstants.Normalize(application.Status),
            DocumentVerificationStatus = GetDocumentVerificationStatus(application.Status),
            CreatedDate = application.CreatedAt
        };
    }

    public async Task<ApplicationStatusResponseDto> GetApplicationStatusAsync(Guid applicationId)
    {
        var currentStatus = await _applicationClient.GetCurrentStatusAsync(applicationId);
        if (currentStatus is null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        return new ApplicationStatusResponseDto
        {
            ApplicationId = applicationId,
            CurrentStatus = AdminStatusConstants.Normalize(currentStatus)
        };
    }

    public async Task<IReadOnlyList<ApplicationStatusHistoryItemDto>> GetApplicationStatusHistoryAsync(Guid applicationId)
    {
        var historyItems = await _statusHistoryRepository.GetByApplicationIdAsync(applicationId);

        if (historyItems.Count == 0)
        {
            var currentStatus = await _applicationClient.GetCurrentStatusAsync(applicationId);
            if (!string.IsNullOrWhiteSpace(currentStatus))
            {
                var normalizedStatus = AdminStatusConstants.Normalize(currentStatus);
                return
                [
                    new ApplicationStatusHistoryItemDto
                    {
                        HistoryId = Guid.Empty,
                        ApplicationId = applicationId,
                        AdminUserId = Guid.Empty,
                        FromStatus = normalizedStatus,
                        ToStatus = normalizedStatus,
                        ChangedBy = "SYSTEM",
                        Remarks = "Current status snapshot.",
                        ChangedAt = DateTime.UtcNow
                    }
                ];
            }
        }

        return historyItems.Select(x => new ApplicationStatusHistoryItemDto
        {
            HistoryId = x.HistoryId,
            ApplicationId = x.ApplicationId,
            AdminUserId = x.AdminUserId,
            FromStatus = x.FromStatus,
            ToStatus = x.ToStatus,
            ChangedBy = x.ChangedBy,
            Remarks = x.Remarks,
            ChangedAt = x.ChangedAt
        }).ToList();
    }

    public async Task<DecisionResponseDto> CreateDecisionAsync(Guid applicationId, CreateDecisionRequestDto request, Guid adminUserId, string adminIdentity)
    {
        var normalizedDecision = AdminStatusConstants.Normalize(request.Decision);
        if (!AdminStatusConstants.DecisionAllowed.Contains(normalizedDecision))
        {
            throw new ArgumentException("Decision must be one of: UNDER_REVIEW, APPROVED, REJECTED.");
        }

        var currentStatus = await _applicationClient.GetCurrentStatusAsync(applicationId);
        if (currentStatus is null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        var normalizedCurrentStatus = AdminStatusConstants.Normalize(currentStatus);

        var updated = await _applicationClient.UpdateStatusAsync(
            applicationId,
            AdminStatusConstants.ToApplicationServiceStatus(normalizedDecision),
            request.Remarks);
        if (!updated)
        {
            throw new InvalidOperationException("Unable to update application status for decision.");
        }

        await RecordStatusHistoryAsync(
            applicationId,
            adminUserId,
            adminIdentity,
            normalizedCurrentStatus,
            normalizedDecision,
            request.Remarks);

        var existingDecision = await _decisionRepository.GetByApplicationIdAsync(applicationId);
        if (existingDecision is null)
        {
            existingDecision = new Decision
            {
                ApplicationId = applicationId,
                AdminUserId = adminUserId,
                DecisionStatus = normalizedDecision,
                Remarks = request.Remarks,
                SanctionAmount = request.SanctionAmount,
                InterestRate = request.InterestRate,
                DecidedBy = adminIdentity,
                DecidedAt = DateTime.UtcNow
            };

            await _decisionRepository.AddAsync(existingDecision);
        }
        else
        {
            existingDecision.AdminUserId = adminUserId;
            existingDecision.DecisionStatus = normalizedDecision;
            existingDecision.Remarks = request.Remarks;
            existingDecision.SanctionAmount = request.SanctionAmount;
            existingDecision.InterestRate = request.InterestRate;
            existingDecision.DecidedBy = adminIdentity;
            existingDecision.DecidedAt = DateTime.UtcNow;

            await _decisionRepository.UpdateAsync(existingDecision);
        }

        await _decisionRepository.SaveChangesAsync();

        if (normalizedDecision is AdminStatusConstants.Approved or AdminStatusConstants.Rejected)
        {
            var applicationDetails = await _applicationClient.GetApplicationByIdAsync(applicationId)
                ?? throw new KeyNotFoundException("Application not found.");

            var sanctionAmount = normalizedDecision == AdminStatusConstants.Approved
                ? request.SanctionAmount ?? 0m
                : 0m;

            var interestRate = normalizedDecision == AdminStatusConstants.Approved
                ? Convert.ToDouble(request.InterestRate ?? 0m)
                : 0d;

            await _publishEndpoint.Publish(new DecisionMadeEvent
            {
                ApplicationId = existingDecision.ApplicationId,
                ApplicantName = applicationDetails.FullName,
                ApplicantEmail = applicationDetails.Email,
                Decision = normalizedDecision,
                Remarks = request.Remarks,
                SanctionAmount = sanctionAmount,
                InterestRate = interestRate,
                DecidedAt = existingDecision.DecidedAt
            });
        }

        return new DecisionResponseDto
        {
            DecisionId = existingDecision.DecisionId,
            ApplicationId = existingDecision.ApplicationId,
            Decision = existingDecision.DecisionStatus,
            Remarks = existingDecision.Remarks,
            SanctionAmount = existingDecision.SanctionAmount,
            InterestRate = existingDecision.InterestRate,
            DecidedAt = existingDecision.DecidedAt
        };
    }

    public async Task<DocumentVerificationResponseDto> VerifyDocumentAsync(Guid documentId, VerifyDocumentRequestDto request, Guid adminUserId, string adminIdentity)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Invalid admin user identifier.");
        }

        if (request is null)
        {
            throw new ArgumentException("Request cannot be null.");
        }

        var document = await _documentClient.GetDocumentByIdAsync(documentId);
        if (document is null)
        {
            throw new KeyNotFoundException("Document not found.");
        }

        var applicationId = document.ApplicationId;
        var statusBefore = AdminStatusConstants.Normalize(await _applicationClient.GetCurrentStatusAsync(applicationId) ?? "UNKNOWN");

        var result = await _documentClient.VerifyDocumentAsync(documentId, request);
        if (result is null)
        {
            throw new KeyNotFoundException("Document not found.");
        }

        var statusAfter = AdminStatusConstants.Normalize(await _applicationClient.GetCurrentStatusAsync(applicationId) ?? "UNKNOWN");

        await RecordStatusHistoryAsync(
            applicationId,
            adminUserId,
            adminIdentity,
            statusBefore,
            statusAfter,
            request.Remarks);

        await _statusHistoryRepository.SaveChangesAsync();

        return result;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var applications = await _applicationClient.GetApplicationsAsync(null);
        var normalizedStatuses = applications.Select(x => AdminStatusConstants.Normalize(x.Status)).ToList();

        return new AdminDashboardDto
        {
            TotalApplications = applications.Count,
            SubmittedCount = normalizedStatuses.Count(x => x == AdminStatusConstants.Submitted),
            DocsPendingCount = normalizedStatuses.Count(x => x == AdminStatusConstants.DocsPending),
            DocsVerifiedCount = normalizedStatuses.Count(x => x == AdminStatusConstants.DocsVerified),
            UnderReviewCount = normalizedStatuses.Count(x => x == AdminStatusConstants.UnderReview),
            ApprovedCount = normalizedStatuses.Count(x => x == AdminStatusConstants.Approved),
            RejectedCount = normalizedStatuses.Count(x => x == AdminStatusConstants.Rejected)
        };
    }

    private static string GetDocumentVerificationStatus(string status)
    {
        var normalized = AdminStatusConstants.Normalize(status);
        return normalized switch
        {
            AdminStatusConstants.DocsVerified => "Verified",
            AdminStatusConstants.UnderReview => "Verified",
            AdminStatusConstants.Approved => "Verified",
            AdminStatusConstants.Rejected => "Verified",
            AdminStatusConstants.Closed => "Verified",
            AdminStatusConstants.DocsPending => "Pending",
            AdminStatusConstants.Submitted => "Pending",
            _ => "NotAvailable"
        };
    }

    private async Task RecordStatusHistoryAsync(
        Guid applicationId,
        Guid adminUserId,
        string adminIdentity,
        string fromStatus,
        string toStatus,
        string? remarks)
    {
        var historyItem = new ApplicationStatusHistory
        {
            ApplicationId = applicationId,
            AdminUserId = adminUserId,
            FromStatus = AdminStatusConstants.Normalize(fromStatus),
            ToStatus = AdminStatusConstants.Normalize(toStatus),
            ChangedBy = adminIdentity,
            Remarks = remarks,
            ChangedAt = DateTime.UtcNow
        };

        await _statusHistoryRepository.AddAsync(historyItem);
    }
}
