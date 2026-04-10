namespace CapFinLoan.Messaging.Contracts.ApplicationStatus;

public record UpdateApplicationStatusCommand
{
    public Guid ApplicationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Remarks { get; init; }
    public string? CorrelationId { get; init; }
    public string Source { get; init; } = "DocumentService";
}