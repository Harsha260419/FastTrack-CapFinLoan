using Microsoft.AspNetCore.Http;

namespace CapFinLoan.Document.Application.DTOs;

public class UploadDocumentDto
{
    public Guid ApplicationId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}
