namespace CapFinLoan.Application.Application.DTOs;

public class ApplicationStatusDto
{
    public Guid ApplicationId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public List<StatusTimelineEntry> Timeline { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

public class StatusTimelineEntry
{
    public string Status { get; set; } = string.Empty;
    public DateTime TransitionDate { get; set; }
    public string? Remarks { get; set; }
    public string? NextAction { get; set; }
}
