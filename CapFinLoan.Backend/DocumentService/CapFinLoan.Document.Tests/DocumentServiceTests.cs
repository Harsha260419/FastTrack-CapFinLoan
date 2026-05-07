using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Application.Services;
using DocumentEntity = CapFinLoan.Document.Domain.Entities.Document;
using CapFinLoan.Document.Domain.Enums;
using CapFinLoan.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.Document.Tests;

[TestFixture]
public class DocumentServiceTests
{
    private Mock<IDocumentRepository> _documentRepository = null!;
    private Mock<IFileStorageService> _fileStorageService = null!;
    private Mock<IApplicationServiceClient> _applicationServiceClient = null!;
    private Mock<IPublishEndpoint> _publishEndpoint = null!;
    private Mock<ILogger<DocumentService>> _logger = null!;
    private DocumentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _documentRepository = new Mock<IDocumentRepository>(MockBehavior.Strict);
        _fileStorageService = new Mock<IFileStorageService>(MockBehavior.Strict);
        _applicationServiceClient = new Mock<IApplicationServiceClient>(MockBehavior.Strict);
        _publishEndpoint = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        _logger = new Mock<ILogger<DocumentService>>();

        _service = new DocumentService(
            _documentRepository.Object,
            _fileStorageService.Object,
            _applicationServiceClient.Object,
            _publishEndpoint.Object,
            _logger.Object);
    }

    [Test]
    public async Task SyncApplicationStatus_AllDocsVerified_SetsDocsVerified()
    {
        var applicationId = Guid.NewGuid();
        var document = CreateDocument(applicationId, DocumentType.IdProof, DocumentStatus.Verified);

        _documentRepository.Setup(x => x.GetByIdAsync(document.Id)).ReturnsAsync(document);
        _documentRepository.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync(CreateAllDocuments(applicationId, DocumentStatus.Verified));
        _applicationServiceClient.Setup(x => x.UpdateDocumentStatusAsync(
            It.Is<ApplicationDocumentStatusUpdateDto>(dto => dto.Status == "DocsVerified"),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publishEndpoint.Setup(x => x.Publish(It.IsAny<DocumentsVerifiedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.VerifyDocumentAsync(Guid.NewGuid(), document.Id, new VerifyDocumentDto { Status = "Verified" }, null, CancellationToken.None);

        _applicationServiceClient.VerifyAll();
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<DocumentsVerifiedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncApplicationStatus_SomeDocsPending_SetsDocsPending()
    {
        var applicationId = Guid.NewGuid();
        var document = CreateDocument(applicationId, DocumentType.IdProof, DocumentStatus.Verified);

        _documentRepository.Setup(x => x.GetByIdAsync(document.Id)).ReturnsAsync(document);
        _documentRepository.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync(CreateMixedDocuments(applicationId));
        _applicationServiceClient.Setup(x => x.UpdateDocumentStatusAsync(
            It.Is<ApplicationDocumentStatusUpdateDto>(dto => dto.Status == "DocsPending"),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.VerifyDocumentAsync(Guid.NewGuid(), document.Id, new VerifyDocumentDto { Status = "Verified" }, null, CancellationToken.None);

        _applicationServiceClient.VerifyAll();
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<DocumentsVerifiedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SyncApplicationStatus_AnyDocRejected_SetsDocsPending()
    {
        var applicationId = Guid.NewGuid();
        var document = CreateDocument(applicationId, DocumentType.IdProof, DocumentStatus.Verified);

        _documentRepository.Setup(x => x.GetByIdAsync(document.Id)).ReturnsAsync(document);
        _documentRepository.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync(CreateRejectedDocuments(applicationId));
        _applicationServiceClient.Setup(x => x.UpdateDocumentStatusAsync(
            It.Is<ApplicationDocumentStatusUpdateDto>(dto => dto.Status == "DocsPending"),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.VerifyDocumentAsync(Guid.NewGuid(), document.Id, new VerifyDocumentDto { Status = "Verified" }, null, CancellationToken.None);

        _applicationServiceClient.VerifyAll();
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<DocumentsVerifiedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task VerifyDocument_UpdatesStatusToVerified()
    {
        var applicationId = Guid.NewGuid();
        var document = CreateDocument(applicationId, DocumentType.IdProof, DocumentStatus.Pending);

        _documentRepository.Setup(x => x.GetByIdAsync(document.Id)).ReturnsAsync(document);
        _documentRepository.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync(CreateAllDocuments(applicationId, DocumentStatus.Verified));
        _applicationServiceClient.Setup(x => x.UpdateDocumentStatusAsync(
            It.IsAny<ApplicationDocumentStatusUpdateDto>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publishEndpoint.Setup(x => x.Publish(It.IsAny<DocumentsVerifiedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.VerifyDocumentAsync(Guid.NewGuid(), document.Id, new VerifyDocumentDto { Status = "Verified" }, null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(DocumentStatus.Verified.ToString()));
        Assert.That(document.Status, Is.EqualTo(DocumentStatus.Verified));
        _documentRepository.VerifyAll();
    }

    [Test]
    public async Task RejectDocument_UpdatesStatusToRejected()
    {
        var applicationId = Guid.NewGuid();
        var document = CreateDocument(applicationId, DocumentType.IdProof, DocumentStatus.Pending);

        _documentRepository.Setup(x => x.GetByIdAsync(document.Id)).ReturnsAsync(document);
        _documentRepository.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _documentRepository.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync(CreateRejectedDocuments(applicationId));
        _applicationServiceClient.Setup(x => x.UpdateDocumentStatusAsync(
            It.IsAny<ApplicationDocumentStatusUpdateDto>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.VerifyDocumentAsync(
            Guid.NewGuid(),
            document.Id,
            new VerifyDocumentDto { Status = "Rejected", Remarks = "invalid" },
            null,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(DocumentStatus.Rejected.ToString()));
        Assert.That(document.Status, Is.EqualTo(DocumentStatus.Rejected));
        Assert.That(document.Remarks, Is.EqualTo("invalid"));
        _documentRepository.VerifyAll();
    }

    private static DocumentEntity CreateDocument(Guid applicationId, DocumentType type, DocumentStatus status)
    {
        return new DocumentEntity
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            UserId = Guid.NewGuid(),
            DocumentType = type,
            Status = status,
            FileName = "file.pdf",
            FilePath = "/tmp/file.pdf",
            UploadedAt = DateTime.UtcNow
        };
    }

    private static List<DocumentEntity> CreateAllDocuments(Guid applicationId, DocumentStatus status)
    {
        return new List<DocumentEntity>
        {
            CreateDocument(applicationId, DocumentType.IdProof, status),
            CreateDocument(applicationId, DocumentType.AddressProof, status),
            CreateDocument(applicationId, DocumentType.BankStatement, status),
            CreateDocument(applicationId, DocumentType.IncomeProof, status)
        };
    }

    private static List<DocumentEntity> CreateMixedDocuments(Guid applicationId)
    {
        return new List<DocumentEntity>
        {
            CreateDocument(applicationId, DocumentType.IdProof, DocumentStatus.Verified),
            CreateDocument(applicationId, DocumentType.AddressProof, DocumentStatus.Pending),
            CreateDocument(applicationId, DocumentType.BankStatement, DocumentStatus.Verified),
            CreateDocument(applicationId, DocumentType.IncomeProof, DocumentStatus.Verified)
        };
    }

    private static List<DocumentEntity> CreateRejectedDocuments(Guid applicationId)
    {
        return new List<DocumentEntity>
        {
            CreateDocument(applicationId, DocumentType.IdProof, DocumentStatus.Rejected),
            CreateDocument(applicationId, DocumentType.AddressProof, DocumentStatus.Verified),
            CreateDocument(applicationId, DocumentType.BankStatement, DocumentStatus.Verified),
            CreateDocument(applicationId, DocumentType.IncomeProof, DocumentStatus.Verified)
        };
    }
}
