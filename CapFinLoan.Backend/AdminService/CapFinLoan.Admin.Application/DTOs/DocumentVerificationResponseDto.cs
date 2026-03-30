namespace CapFinLoan.Admin.Application.DTOs;

public class DocumentVerificationResponseDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime UploadedAt { get; set; }
}
