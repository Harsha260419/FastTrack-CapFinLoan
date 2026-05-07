using CapFinLoan.Admin.Application.Constants;
using CapFinLoan.Admin.Application.DTOs;
using CapFinLoan.Admin.Application.Exceptions;
using CapFinLoan.Admin.Application.Interfaces;
using CapFinLoan.Admin.Application.Services;
using CapFinLoan.Admin.Domain.Entities;
using CapFinLoan.Messaging.Contracts;
using MassTransit;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.Admin.Tests;

[TestFixture]
public class AdminServiceTests
{
    private Mock<IApplicationClient> _applicationClient = null!;
    private Mock<IDocumentClient> _documentClient = null!;
    private Mock<IDecisionRepository> _decisionRepository = null!;
    private Mock<IStatusHistoryRepository> _statusHistoryRepository = null!;
    private Mock<IPublishEndpoint> _publishEndpoint = null!;
    private AdminService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _applicationClient = new Mock<IApplicationClient>(MockBehavior.Strict);
        _documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
        _decisionRepository = new Mock<IDecisionRepository>(MockBehavior.Strict);
        _statusHistoryRepository = new Mock<IStatusHistoryRepository>(MockBehavior.Strict);
        _publishEndpoint = new Mock<IPublishEndpoint>(MockBehavior.Strict);

        _service = new AdminService(
            _applicationClient.Object,
            _documentClient.Object,
            _decisionRepository.Object,
            _statusHistoryRepository.Object,
            _publishEndpoint.Object);
    }

    [Test]
    public async Task MakeDecision_Approve_PublishesDecisionMadeEvent()
    {
        var applicationId = Guid.NewGuid();
        var request = new CreateDecisionRequestDto
        {
            Decision = AdminStatusConstants.Approved,
            Remarks = "ok",
            SanctionAmount = 100000,
            InterestRate = 10.5m
        };

        _applicationClient.Setup(x => x.GetCurrentStatusAsync(applicationId)).ReturnsAsync(AdminStatusConstants.UnderReview);
        _applicationClient.Setup(x => x.UpdateStatusAsync(applicationId, "Approved", request.Remarks)).ReturnsAsync(true);
        _statusHistoryRepository.Setup(x => x.AddAsync(It.IsAny<ApplicationStatusHistory>())).Returns(Task.CompletedTask);
        _decisionRepository.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync((Decision?)null);
        _decisionRepository.Setup(x => x.AddAsync(It.IsAny<Decision>())).Returns(Task.CompletedTask);
        _decisionRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _applicationClient.Setup(x => x.GetApplicationByIdAsync(applicationId)).ReturnsAsync(new ApplicationServiceApplicationDto
        {
            ApplicationId = applicationId,
            FullName = "User",
            Email = "user@example.com",
            Status = AdminStatusConstants.UnderReview
        });
        _publishEndpoint.Setup(x => x.Publish(It.IsAny<DecisionMadeEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateDecisionAsync(applicationId, request, Guid.NewGuid(), "ADMIN");

        Assert.That(result.Decision, Is.EqualTo(AdminStatusConstants.Approved));
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<DecisionMadeEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _decisionRepository.VerifyAll();
        _statusHistoryRepository.VerifyAll();
    }

    [Test]
    public async Task MakeDecision_Reject_PublishesDecisionMadeEvent()
    {
        var applicationId = Guid.NewGuid();
        var request = new CreateDecisionRequestDto
        {
            Decision = AdminStatusConstants.Rejected,
            Remarks = "no",
            SanctionAmount = 0,
            InterestRate = 0
        };

        _applicationClient.Setup(x => x.GetCurrentStatusAsync(applicationId)).ReturnsAsync(AdminStatusConstants.UnderReview);
        _applicationClient.Setup(x => x.UpdateStatusAsync(applicationId, "Rejected", request.Remarks)).ReturnsAsync(true);
        _statusHistoryRepository.Setup(x => x.AddAsync(It.IsAny<ApplicationStatusHistory>())).Returns(Task.CompletedTask);
        _decisionRepository.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync((Decision?)null);
        _decisionRepository.Setup(x => x.AddAsync(It.IsAny<Decision>())).Returns(Task.CompletedTask);
        _decisionRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _applicationClient.Setup(x => x.GetApplicationByIdAsync(applicationId)).ReturnsAsync(new ApplicationServiceApplicationDto
        {
            ApplicationId = applicationId,
            FullName = "User",
            Email = "user@example.com",
            Status = AdminStatusConstants.UnderReview
        });
        _publishEndpoint.Setup(x => x.Publish(It.IsAny<DecisionMadeEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateDecisionAsync(applicationId, request, Guid.NewGuid(), "ADMIN");

        Assert.That(result.Decision, Is.EqualTo(AdminStatusConstants.Rejected));
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<DecisionMadeEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _decisionRepository.VerifyAll();
        _statusHistoryRepository.VerifyAll();
    }

    [Test]
    public void MakeDecision_WithInvalidStatus_ThrowsOrReturnsFailure()
    {
        var applicationId = Guid.NewGuid();
        var request = new CreateDecisionRequestDto
        {
            Decision = AdminStatusConstants.Approved,
            Remarks = "ok"
        };

        _applicationClient.Setup(x => x.GetCurrentStatusAsync(applicationId)).ReturnsAsync(AdminStatusConstants.Approved);
        _applicationClient.Setup(x => x.UpdateStatusAsync(applicationId, "Approved", request.Remarks)).ReturnsAsync(false);

        Assert.ThrowsAsync<ConflictException>(() => _service.CreateDecisionAsync(applicationId, request, Guid.NewGuid(), "ADMIN"));

        _applicationClient.VerifyAll();
    }

    [Test]
    public async Task GetDashboard_ReturnCorrectCounts()
    {
        _applicationClient.Setup(x => x.GetApplicationsAsync(null)).ReturnsAsync(new List<ApplicationServiceApplicationDto>
        {
            new() { Status = "APPROVED" },
            new() { Status = "REJECTED" },
            new() { Status = "DOCS_PENDING" },
            new() { Status = "DOCS_VERIFIED" },
            new() { Status = "SUBMITTED" },
            new() { Status = "UNDER_REVIEW" }
        });

        var result = await _service.GetDashboardAsync();

        Assert.That(result.TotalApplications, Is.EqualTo(6));
        Assert.That(result.ApprovedCount, Is.EqualTo(1));
        Assert.That(result.RejectedCount, Is.EqualTo(1));
        Assert.That(result.DocsPendingCount, Is.EqualTo(1));
        Assert.That(result.DocsVerifiedCount, Is.EqualTo(1));
        Assert.That(result.SubmittedCount, Is.EqualTo(1));
        Assert.That(result.UnderReviewCount, Is.EqualTo(1));

        _applicationClient.VerifyAll();
    }
}
