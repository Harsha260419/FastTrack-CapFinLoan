namespace CapFinLoan.Document.Application.DTOs;

public class ApplicationDocumentStatusUpdateDto
{
    public Guid ApplicationId { get; set; }
    public string Status { get; set; } = string.Empty;
}
