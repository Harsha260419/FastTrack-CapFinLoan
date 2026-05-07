using CapFinLoan.Application.Application.DTOs;
using CapFinLoan.Application.Application.Exceptions;
using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Application.Services;
using CapFinLoan.Application.Domain.Entities;
using CapFinLoan.Application.Domain.Enums;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.Application.Tests;

[TestFixture]
public class LoanApplicationServiceTests
{
    private Mock<ILoanApplicationRepository> _repository = null!;
    private Mock<IAdminStatusHistoryClient> _adminStatusHistoryClient = null!;
    private LoanApplicationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<ILoanApplicationRepository>(MockBehavior.Strict);
        _adminStatusHistoryClient = new Mock<IAdminStatusHistoryClient>(MockBehavior.Strict);
        _service = new LoanApplicationService(_repository.Object, _adminStatusHistoryClient.Object);
    }

    [Test]
    public async Task SubmitApplication_WithDraftStatus_ChangesStatusToSubmitted()
    {
        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var application = CreateValidApplication(userId, applicationId, ApplicationStatus.Draft);

        _repository.Setup(x => x.GetByIdAsync(applicationId)).ReturnsAsync(application);
        _repository.Setup(x => x.UpdateAsync(application)).Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.SubmitApplicationAsync(userId, applicationId, new SubmitApplicationRequestDto
        {
            DeclarationAccepted = true,
            Comments = "Ready"
        });

        Assert.That(result.Status, Is.EqualTo(ApplicationStatus.Submitted.ToString()));
        _repository.VerifyAll();
    }

    [Test]
    public void SubmitApplication_WithNonDraftStatus_ThrowsOrReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var application = CreateValidApplication(userId, applicationId, ApplicationStatus.Submitted);

        _repository.Setup(x => x.GetByIdAsync(applicationId)).ReturnsAsync(application);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.SubmitApplicationAsync(userId, applicationId, new SubmitApplicationRequestDto { DeclarationAccepted = true }));

        _repository.VerifyAll();
    }

    [Test]
    public void SubmitApplication_WithNonExistentId_ThrowsOrReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        _repository.Setup(x => x.GetByIdAsync(applicationId)).ReturnsAsync((LoanApplication?)null);

        Assert.ThrowsAsync<NotFoundException>(() =>
            _service.SubmitApplicationAsync(userId, applicationId, new SubmitApplicationRequestDto { DeclarationAccepted = true }));

        _repository.VerifyAll();
    }

    [Test]
    public async Task UpdateStatus_WithValidTransition_UpdatesStatus()
    {
        var applicationId = Guid.NewGuid();
        var application = CreateValidApplication(Guid.NewGuid(), applicationId, ApplicationStatus.DocsPending);

        _repository.Setup(x => x.GetByIdAsync(applicationId)).ReturnsAsync(application);
        _repository.Setup(x => x.UpdateAsync(application)).Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.UpdateApplicationStatusInternalAsync(applicationId, new UpdateApplicationStatusInternalRequestDto
        {
            Status = "DocsVerified",
            Remarks = "ok"
        });

        Assert.That(result.Status, Is.EqualTo(ApplicationStatus.DocsVerified.ToString()));
        _repository.VerifyAll();
    }

    [Test]
    public void UpdateStatus_WithInvalidTransition_ThrowsOrReturnsFailure()
    {
        var applicationId = Guid.NewGuid();
        var application = CreateValidApplication(Guid.NewGuid(), applicationId, ApplicationStatus.Draft);

        _repository.Setup(x => x.GetByIdAsync(applicationId)).ReturnsAsync(application);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateApplicationStatusInternalAsync(applicationId, new UpdateApplicationStatusInternalRequestDto
            {
                Status = "Approved",
                Remarks = "bad"
            }));

        _repository.VerifyAll();
    }

    [Test]
    public async Task GetMyApplications_ReturnsOnlyUserApplications()
    {
        var userId = Guid.NewGuid();
        var applications = new List<LoanApplication>
        {
            CreateValidApplication(userId, Guid.NewGuid(), ApplicationStatus.Draft),
            CreateValidApplication(userId, Guid.NewGuid(), ApplicationStatus.Submitted)
        };

        _repository.Setup(x => x.GetByUserIdPaginatedAsync(userId, 1, 10))
            .ReturnsAsync((applications, applications.Count));

        var (result, total) = await _service.GetMyApplicationsAsync(userId, 1, 10);

        Assert.That(result.Count, Is.EqualTo(applications.Count));
        Assert.That(total, Is.EqualTo(applications.Count));
        _repository.VerifyAll();
    }

    private static LoanApplication CreateValidApplication(Guid userId, Guid applicationId, ApplicationStatus status)
    {
        return new LoanApplication
        {
            ApplicationId = applicationId,
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male",
            AddressLine1 = "123 Main",
            AddressLine2 = "",
            City = "City",
            State = "State",
            PostalCode = "12345",
            EmployerName = "Employer",
            EmploymentType = "FullTime",
            MonthlyIncome = 50000,
            AnnualIncome = 600000,
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "9999999999",
            LoanAmount = 100000,
            LoanPurpose = "Home",
            TenureMonths = 12,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
