namespace CapFinLoan.Messaging.Contracts.ApplicationStatus;

public record UpdateApplicationStatusResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UpdatedStatus { get; init; }
}