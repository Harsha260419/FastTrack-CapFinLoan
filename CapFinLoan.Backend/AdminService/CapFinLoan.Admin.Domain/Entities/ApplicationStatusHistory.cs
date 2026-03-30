namespace CapFinLoan.Admin.Domain.Entities;

public class ApplicationStatusHistory
{
    public Guid HistoryId { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public Guid AdminUserId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
