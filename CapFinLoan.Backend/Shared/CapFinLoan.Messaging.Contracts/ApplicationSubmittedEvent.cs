namespace CapFinLoan.Messaging.Contracts;

public record ApplicationSubmittedEvent
{
    public Guid ApplicationId { get; init; }
    public Guid UserId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public string Source { get; init; } = "ApplicationService";
}