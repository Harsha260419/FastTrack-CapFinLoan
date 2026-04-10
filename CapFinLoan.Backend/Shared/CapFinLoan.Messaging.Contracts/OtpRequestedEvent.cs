namespace CapFinLoan.Messaging.Contracts;

public record OtpRequestedEvent
{
    public string Email { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
    public string ApplicantName { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}