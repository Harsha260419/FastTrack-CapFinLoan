using CapFinLoan.Application.Application.DTOs;
using CapFinLoan.Application.Application.Exceptions;
using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Domain.Entities;
using CapFinLoan.Application.Domain.Enums;

namespace CapFinLoan.Application.Application.Services;

public class LoanApplicationService : ILoanApplicationService
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IAdminStatusHistoryClient _adminStatusHistoryClient;

    public LoanApplicationService(
        ILoanApplicationRepository repository,
        IAdminStatusHistoryClient adminStatusHistoryClient)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _adminStatusHistoryClient = adminStatusHistoryClient ?? throw new ArgumentNullException(nameof(adminStatusHistoryClient));
    }

    public async Task<ApplicationResponseDto> CreateApplicationAsync(Guid userId, CreateApplicationRequestDto request)
    {
        ValidateCreateApplicationRequest(request);

        var fullName = $"{request.PersonalDetails.FirstName} {request.PersonalDetails.LastName}".Trim();

        var application = new LoanApplication
        {
            ApplicationId = Guid.NewGuid(),
            UserId = userId,
            FirstName = request.PersonalDetails.FirstName.Trim(),
            LastName = request.PersonalDetails.LastName.Trim(),
            DateOfBirth = request.PersonalDetails.DateOfBirth,
            Gender = request.PersonalDetails.Gender.Trim(),
            AddressLine1 = request.PersonalDetails.AddressLine1.Trim(),
            AddressLine2 = request.PersonalDetails.AddressLine2.Trim(),
            City = request.PersonalDetails.City.Trim(),
            State = request.PersonalDetails.State.Trim(),
            PostalCode = request.PersonalDetails.PostalCode.Trim(),
            EmployerName = request.EmploymentDetails.EmployerName.Trim(),
            EmploymentType = request.EmploymentDetails.EmploymentType.Trim(),
            MonthlyIncome = request.EmploymentDetails.MonthlyIncome,
            AnnualIncome = request.EmploymentDetails.AnnualIncome,
            FullName = fullName,
            Email = request.PersonalDetails.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PersonalDetails.Phone.Trim(),
            LoanAmount = request.LoanDetails.RequestedAmount,
            LoanPurpose = request.LoanDetails.LoanPurpose.Trim(),
            TenureMonths = request.LoanDetails.RequestedTenureMonths,
            Status = ApplicationStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SubmittedAt = null,
            AdminRemarks = string.IsNullOrWhiteSpace(request.LoanDetails.Remarks)
                ? null
                : request.LoanDetails.Remarks.Trim()
        };

        await _repository.AddAsync(application);
        await _repository.SaveChangesAsync();

        return MapToApplicationResponseDto(application, true, "Application created successfully in Draft status.");
    }

    public async Task<ApplicationResponseDto> UpdateApplicationAsync(Guid userId, Guid applicationId, UpdateApplicationRequestDto request)
    {
        ValidateUpdateApplicationRequest(request);

        var application = await _repository.GetByIdAsync(applicationId);
        if (application == null)
        {
            throw new NotFoundException($"Application with ID {applicationId} not found.");
        }

        if (application.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to update this application.");
        }

        if (application.Status != ApplicationStatus.Draft)
        {
            throw new ConflictException($"Cannot update application in {application.Status} status. Only Draft applications can be updated.");
        }

        var fullName = $"{request.PersonalDetails.FirstName} {request.PersonalDetails.LastName}".Trim();

        application.FirstName = request.PersonalDetails.FirstName.Trim();
        application.LastName = request.PersonalDetails.LastName.Trim();
        application.DateOfBirth = request.PersonalDetails.DateOfBirth;
        application.Gender = request.PersonalDetails.Gender.Trim();
        application.AddressLine1 = request.PersonalDetails.AddressLine1.Trim();
        application.AddressLine2 = request.PersonalDetails.AddressLine2.Trim();
        application.City = request.PersonalDetails.City.Trim();
        application.State = request.PersonalDetails.State.Trim();
        application.PostalCode = request.PersonalDetails.PostalCode.Trim();
        application.EmployerName = request.EmploymentDetails.EmployerName.Trim();
        application.EmploymentType = request.EmploymentDetails.EmploymentType.Trim();
        application.MonthlyIncome = request.EmploymentDetails.MonthlyIncome;
        application.AnnualIncome = request.EmploymentDetails.AnnualIncome;
        application.FullName = fullName;
        application.Email = request.PersonalDetails.Email.Trim().ToLowerInvariant();
        application.PhoneNumber = request.PersonalDetails.Phone.Trim();
        application.LoanAmount = request.LoanDetails.RequestedAmount;
        application.LoanPurpose = request.LoanDetails.LoanPurpose.Trim();
        application.TenureMonths = request.LoanDetails.RequestedTenureMonths;
        application.AdminRemarks = string.IsNullOrWhiteSpace(request.LoanDetails.Remarks)
            ? application.AdminRemarks
            : request.LoanDetails.Remarks.Trim();
        application.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(application);
        await _repository.SaveChangesAsync();

        return MapToApplicationResponseDto(application, true, "Application updated successfully.");
    }

    public async Task<ApplicationResponseDto> SubmitApplicationAsync(Guid userId, Guid applicationId, SubmitApplicationRequestDto request)
    {
        if (request == null)
        {
            throw new ValidationException("Submit request cannot be null.");
        }

        if (!request.DeclarationAccepted)
        {
            throw new ValidationException("Declaration must be accepted to submit application.");
        }

        var application = await _repository.GetByIdAsync(applicationId);
        if (application == null)
        {
            throw new NotFoundException($"Application with ID {applicationId} not found.");
        }

        if (application.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to submit this application.");
        }

        if (application.Status != ApplicationStatus.Draft)
        {
            throw new ConflictException($"Cannot submit application in {application.Status} status. Only Draft applications can be submitted.");
        }

        ValidateApplicationForSubmission(application);

        application.Status = ApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Comments))
        {
            application.AdminRemarks = $"[Submitted] {request.Comments}";
        }

        await _repository.UpdateAsync(application);
        await _repository.SaveChangesAsync();

        return MapToApplicationResponseDto(application, true, "Application submitted successfully. Documents are now pending verification.");
    }

    public async Task<ApplicationResponseDto> DeleteApplicationAsync(Guid userId, Guid applicationId)
    {
        var application = await _repository.GetByIdAsync(applicationId);
        if (application == null)
        {
            throw new NotFoundException($"Application with ID {applicationId} not found.");
        }

        if (application.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to delete this application.");
        }

        if (application.Status != ApplicationStatus.Draft)
        {
            throw new ConflictException("Only Draft applications can be deleted.");
        }

        await _repository.DeleteAsync(application);
        await _repository.SaveChangesAsync();

        return new ApplicationResponseDto
        {
            ApplicationId = applicationId,
            UserId = userId,
            Status = ApplicationStatus.Draft.ToString(),
            Success = true,
            Message = "Application deleted successfully."
        };
    }

    public async Task<(List<ApplicationResponseDto> Applications, int TotalCount)> GetMyApplicationsAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var (applications, totalCount) = await _repository.GetByUserIdPaginatedAsync(userId, pageNumber, pageSize);

        var applicationDtos = applications.Select(app => MapToApplicationResponseDto(app, true, "")).ToList();

        return (applicationDtos, totalCount);
    }

    public async Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid userId, Guid applicationId)
    {
        var application = await _repository.GetByIdAsync(applicationId);
        if (application == null)
        {
            throw new NotFoundException($"Application with ID {applicationId} not found.");
        }

        if (application.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to view this application status.");
        }

        var timeline = await BuildStatusTimelineAsync(application);

        return new ApplicationStatusDto
        {
            ApplicationId = application.ApplicationId,
            CurrentStatus = application.Status.ToString(),
            Timeline = timeline,
            Success = true,
            Message = "Application status timeline retrieved successfully."
        };
    }

    public async Task<ApplicationDetailsResponseDto> GetApplicationDetailsAsync(Guid requesterUserId, bool isAdmin, Guid applicationId)
    {
        var application = await _repository.GetByIdAsync(applicationId)
            ?? throw new NotFoundException($"Application with ID {applicationId} not found.");

        if (!isAdmin && application.UserId != requesterUserId)
        {
            throw new ForbiddenException("You do not have permission to view this application.");
        }

        return MapToApplicationDetailsResponseDto(application);
    }

    public async Task<IReadOnlyList<ApplicationResponseDto>> GetApplicationsForAdminAsync(string? status)
    {
        List<LoanApplication> applications;

        if (string.IsNullOrWhiteSpace(status))
        {
            applications = await GetAllApplicationsAsync();
        }
        else
        {
            var parsedStatus = ParseApplicationStatus(status);
            applications = await _repository.GetByStatusAsync(parsedStatus);
        }

        return applications
            .OrderByDescending(x => x.UpdatedAt)
            .Select(app => MapToApplicationResponseDto(app, true, string.Empty))
            .ToList();
    }

    public async Task<ApplicationResponseDto> GetApplicationByIdForAdminAsync(Guid applicationId)
    {
        var application = await _repository.GetByIdAsync(applicationId)
            ?? throw new NotFoundException($"Application with ID {applicationId} not found.");

        return MapToApplicationResponseDto(application, true, string.Empty);
    }

    public async Task<ApplicationResponseDto> UpdateApplicationStatusInternalAsync(Guid applicationId, UpdateApplicationStatusInternalRequestDto request)
    {
        if (request is null)
        {
            throw new ValidationException("Request cannot be null.");
        }

        var targetStatus = ParseApplicationStatus(request.Status);
        var application = await _repository.GetByIdAsync(applicationId)
            ?? throw new NotFoundException($"Application with ID {applicationId} not found.");

        if (application.Status == targetStatus)
        {
            return MapToApplicationResponseDto(application, true, "Application status already up to date.");
        }

        if (!IsTransitionAllowed(application.Status, targetStatus))
        {
            throw new ConflictException($"Invalid status transition: {application.Status} -> {targetStatus}");
        }

        application.Status = targetStatus;
        application.UpdatedAt = DateTime.UtcNow;

        if (targetStatus == ApplicationStatus.Submitted && application.SubmittedAt is null)
        {
            application.SubmittedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(request.Remarks))
        {
            application.AdminRemarks = request.Remarks.Trim();
        }

        await _repository.UpdateAsync(application);
        await _repository.SaveChangesAsync();

        return MapToApplicationResponseDto(application, true, "Application status updated successfully.");
    }

    private void ValidateCreateApplicationRequest(CreateApplicationRequestDto request)
    {
        if (request == null)
        {
            throw new ValidationException("Request cannot be null.");
        }

        if (request.PersonalDetails == null || request.EmploymentDetails == null || request.LoanDetails == null)
        {
            throw new ValidationException("PersonalDetails, EmploymentDetails, and LoanDetails are required.");
        }

        if (string.IsNullOrWhiteSpace(request.PersonalDetails.FirstName) || string.IsNullOrWhiteSpace(request.PersonalDetails.LastName))
        {
            throw new ValidationException("First name and last name are required.");
        }

        if (string.IsNullOrWhiteSpace(request.PersonalDetails.Email) || !request.PersonalDetails.Email.Contains("@"))
        {
            throw new ValidationException("Valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PersonalDetails.Phone) || request.PersonalDetails.Phone.Length < 10)
        {
            throw new ValidationException("Valid phone number (minimum 10 digits) is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PersonalDetails.AddressLine1) ||
            string.IsNullOrWhiteSpace(request.PersonalDetails.City) ||
            string.IsNullOrWhiteSpace(request.PersonalDetails.State) ||
            string.IsNullOrWhiteSpace(request.PersonalDetails.PostalCode))
        {
            throw new ValidationException("AddressLine1, City, State, and PostalCode are required.");
        }

        if (request.LoanDetails.RequestedAmount <= 0)
        {
            throw new ValidationException("Loan amount must be greater than zero.");
        }

        if (request.LoanDetails.RequestedAmount > 10000000)
        {
            throw new ValidationException("Loan amount exceeds maximum limit.");
        }

        if (string.IsNullOrWhiteSpace(request.LoanDetails.LoanPurpose))
        {
            throw new ValidationException("Loan purpose is required.");
        }

        if (request.LoanDetails.RequestedTenureMonths <= 0 || request.LoanDetails.RequestedTenureMonths > 360)
        {
            throw new ValidationException("Loan tenure must be between 1 and 360 months.");
        }

        if (request.EmploymentDetails.MonthlyIncome < 0 ||
            request.EmploymentDetails.AnnualIncome < 0)
        {
            throw new ValidationException("Income values cannot be negative.");
        }
    }

    private async Task<List<LoanApplication>> GetAllApplicationsAsync()
    {
        var statuses = Enum.GetValues<ApplicationStatus>();
        var all = new List<LoanApplication>();

        foreach (var status in statuses)
        {
            var byStatus = await _repository.GetByStatusAsync(status);
            all.AddRange(byStatus);
        }

        return all;
    }

    private static ApplicationStatus ParseApplicationStatus(string rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            throw new ValidationException("Status is required.");
        }

        if (int.TryParse(rawStatus.Trim(), out _))
        {
            throw new ValidationException("Numeric status is not allowed. Use literal status values.");
        }

        var normalized = rawStatus.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");
        var canonical = normalized switch
        {
            "DRAFT" => nameof(ApplicationStatus.Draft),
            "SUBMITTED" => nameof(ApplicationStatus.Submitted),
            "DOCSPENDING" or "DOCS_PENDING" => nameof(ApplicationStatus.DocsPending),
            "DOCSVERIFIED" or "DOCS_VERIFIED" => nameof(ApplicationStatus.DocsVerified),
            "UNDERREVIEW" or "UNDER_REVIEW" => nameof(ApplicationStatus.UnderReview),
            "APPROVED" => nameof(ApplicationStatus.Approved),
            "REJECTED" => nameof(ApplicationStatus.Rejected),
            "CLOSED" => nameof(ApplicationStatus.Closed),
            _ => rawStatus.Trim()
        };

        if (!Enum.TryParse<ApplicationStatus>(canonical, true, out var parsed))
        {
            throw new ValidationException("Invalid status value.");
        }

        return parsed;
    }

    private static bool IsTransitionAllowed(ApplicationStatus current, ApplicationStatus target)
    {
        return (current, target) switch
        {
            (ApplicationStatus.Submitted, ApplicationStatus.DocsPending) => true,
            (ApplicationStatus.Submitted, ApplicationStatus.DocsVerified) => true,
            (ApplicationStatus.DocsPending, ApplicationStatus.DocsVerified) => true,
            (ApplicationStatus.DocsVerified, ApplicationStatus.DocsPending) => true,
            (ApplicationStatus.DocsVerified, ApplicationStatus.UnderReview) => true,
            (ApplicationStatus.UnderReview, ApplicationStatus.Approved) => true,
            (ApplicationStatus.UnderReview, ApplicationStatus.Rejected) => true,
            (ApplicationStatus.Approved, ApplicationStatus.Closed) => true,
            (ApplicationStatus.Rejected, ApplicationStatus.Closed) => true,
            _ => false
        };
    }

    private void ValidateUpdateApplicationRequest(UpdateApplicationRequestDto request)
    {
        if (request == null)
        {
            throw new ValidationException("Request cannot be null.");
        }

        ValidateCreateApplicationRequest(new CreateApplicationRequestDto
        {
            PersonalDetails = request.PersonalDetails,
            EmploymentDetails = request.EmploymentDetails,
            LoanDetails = request.LoanDetails
        });
    }

    private void ValidateApplicationForSubmission(LoanApplication application)
    {
        // Verify all required fields are populated
        if (string.IsNullOrWhiteSpace(application.FullName) ||
            string.IsNullOrWhiteSpace(application.Email) ||
            string.IsNullOrWhiteSpace(application.PhoneNumber) ||
            application.LoanAmount <= 0 ||
            string.IsNullOrWhiteSpace(application.LoanPurpose) ||
            application.TenureMonths <= 0)
        {
            throw new ValidationException("Application has incomplete information. Please fill all required fields before submitting.");
        }
    }

    private ApplicationResponseDto MapToApplicationResponseDto(LoanApplication application, bool success, string message)
    {
        return new ApplicationResponseDto
        {
            ApplicationId = application.ApplicationId,
            UserId = application.UserId,
            FullName = application.FullName,
            Email = application.Email,
            PhoneNumber = application.PhoneNumber,
            LoanAmount = application.LoanAmount,
            LoanPurpose = application.LoanPurpose,
            TenureMonths = application.TenureMonths,
            Status = application.Status.ToString(),
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt,
            SubmittedAt = application.SubmittedAt,
            AdminRemarks = application.AdminRemarks,
            Success = success,
            Message = message
        };
    }

    private ApplicationDetailsResponseDto MapToApplicationDetailsResponseDto(LoanApplication application)
    {
        return new ApplicationDetailsResponseDto
        {
            ApplicationId = application.ApplicationId,
            Status = application.Status.ToString(),
            PersonalDetails = new ApplicationPersonalDetailsResponseDto
            {
                FirstName = application.FirstName,
                LastName = application.LastName,
                DateOfBirth = application.DateOfBirth,
                Gender = application.Gender,
                Email = application.Email,
                Phone = application.PhoneNumber,
                AddressLine1 = application.AddressLine1,
                AddressLine2 = application.AddressLine2,
                City = application.City,
                State = application.State,
                PostalCode = application.PostalCode
            },
            EmploymentDetails = new ApplicationEmploymentDetailsResponseDto
            {
                EmployerName = application.EmployerName,
                EmploymentType = application.EmploymentType,
                MonthlyIncome = application.MonthlyIncome,
                AnnualIncome = application.AnnualIncome
            },
            LoanDetails = new ApplicationLoanDetailsResponseDto
            {
                RequestedAmount = application.LoanAmount,
                RequestedTenureMonths = application.TenureMonths,
                LoanPurpose = application.LoanPurpose,
                Remarks = application.AdminRemarks ?? string.Empty
            }
        };
    }

    private async Task<List<StatusTimelineEntry>> BuildStatusTimelineAsync(LoanApplication application)
    {
        var timeline = new List<StatusTimelineEntry>
        {
            new StatusTimelineEntry
            {
                Status = ApplicationStatus.Draft.ToString(),
                TransitionDate = application.CreatedAt,
                Remarks = "Application created in draft",
                NextAction = "Save and submit your application"
            }
        };

        var historyRows = await _adminStatusHistoryClient.GetStatusHistoryAsync(application.ApplicationId);
        if (historyRows.Count == 0)
        {
            historyRows = await _repository.GetStatusHistoryByApplicationIdAsync(application.ApplicationId);
        }

        var reachedStatuses = new Dictionary<ApplicationStatus, (DateTime ChangedAt, string? Remarks)>();

        foreach (var row in historyRows.OrderBy(x => x.ChangedAt))
        {
            if (!TryParseApplicationStatus(row.ToStatus, out var parsedStatus))
            {
                continue;
            }

            if (parsedStatus == ApplicationStatus.Draft)
            {
                continue;
            }

            if (!reachedStatuses.ContainsKey(parsedStatus))
            {
                reachedStatuses[parsedStatus] = (row.ChangedAt, row.Remarks);
            }
        }

        if (!reachedStatuses.ContainsKey(ApplicationStatus.Submitted) && application.SubmittedAt.HasValue)
        {
            reachedStatuses[ApplicationStatus.Submitted] = (application.SubmittedAt.Value, "Application submitted for review");
        }

        if (application.Status != ApplicationStatus.Draft && !reachedStatuses.ContainsKey(application.Status))
        {
            reachedStatuses[application.Status] = (application.UpdatedAt, null);
        }

        foreach (var reached in reachedStatuses.OrderBy(x => x.Value.ChangedAt))
        {
            timeline.Add(new StatusTimelineEntry
            {
                Status = reached.Key.ToString(),
                TransitionDate = reached.Value.ChangedAt,
                Remarks = string.IsNullOrWhiteSpace(reached.Value.Remarks)
                    ? GetStatusRemarks(reached.Key)
                    : reached.Value.Remarks,
                NextAction = GetNextActionForStatus(reached.Key)
            });
        }

        return timeline;
    }

    private static bool TryParseApplicationStatus(string rawStatus, out ApplicationStatus parsedStatus)
    {
        parsedStatus = default;

        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return false;
        }

        if (int.TryParse(rawStatus.Trim(), out _))
        {
            return false;
        }

        var normalized = rawStatus.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");
        var canonical = normalized switch
        {
            "DRAFT" => nameof(ApplicationStatus.Draft),
            "SUBMITTED" => nameof(ApplicationStatus.Submitted),
            "DOCSPENDING" or "DOCS_PENDING" => nameof(ApplicationStatus.DocsPending),
            "DOCSVERIFIED" or "DOCS_VERIFIED" => nameof(ApplicationStatus.DocsVerified),
            "UNDERREVIEW" or "UNDER_REVIEW" => nameof(ApplicationStatus.UnderReview),
            "APPROVED" => nameof(ApplicationStatus.Approved),
            "REJECTED" => nameof(ApplicationStatus.Rejected),
            "CLOSED" => nameof(ApplicationStatus.Closed),
            _ => rawStatus.Trim()
        };

        return Enum.TryParse(canonical, true, out parsedStatus);
    }

    private string GetStatusRemarks(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.DocsPending => "Awaiting document upload and verification",
            ApplicationStatus.DocsVerified => "Documents have been verified",
            ApplicationStatus.UnderReview => "Your application is under review by admin",
            ApplicationStatus.Approved => "Congratulations! Your application has been approved",
            ApplicationStatus.Rejected => "Your application has been rejected",
            ApplicationStatus.Closed => "Your application has been closed",
            _ => "Application status updated"
        };
    }

    private string? GetNextActionForStatus(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.DocsPending => "Upload KYC and income documents",
            ApplicationStatus.DocsVerified => "Wait for admin review",
            ApplicationStatus.UnderReview => "Decision will be notified within 3-5 business days",
            ApplicationStatus.Approved => "Download loan approval letter from documents",
            ApplicationStatus.Rejected => "Contact customer support for details",
            ApplicationStatus.Closed => null,
            _ => null
        };
    }
}
