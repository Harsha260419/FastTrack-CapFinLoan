namespace CapFinLoan.Admin.Application.DTOs;

public class ApplicationStatusHistoryItemDto
{
    public Guid HistoryId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid AdminUserId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime ChangedAt { get; set; }
}
