namespace CapFinLoan.Admin.Domain.Entities;

public class Decision
{
    public Guid DecisionId { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public Guid AdminUserId { get; set; }
    public string DecisionStatus { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public decimal? SanctionAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public string DecidedBy { get; set; } = string.Empty;
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
