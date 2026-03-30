using CapFinLoan.Document.Domain.Enums;

namespace CapFinLoan.Document.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? Remarks { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
