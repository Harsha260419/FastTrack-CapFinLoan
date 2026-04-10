namespace CapFinLoan.Messaging.Contracts;

public record DecisionMadeEvent
{
    public Guid ApplicationId { get; init; }
    public string ApplicantName { get; init; } = string.Empty;
    public string ApplicantEmail { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty;
    public string Remarks { get; init; } = string.Empty;
    public decimal SanctionAmount { get; init; }
    public double InterestRate { get; init; }
    public DateTime DecidedAt { get; init; }
}