namespace CapFinLoan.Admin.Application.DTOs;

public class DecisionResponseDto
{
    public Guid DecisionId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public decimal? SanctionAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public DateTime DecidedAt { get; set; }
}
