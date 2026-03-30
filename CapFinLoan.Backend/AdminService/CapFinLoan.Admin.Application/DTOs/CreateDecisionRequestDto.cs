namespace CapFinLoan.Admin.Application.DTOs;

public class CreateDecisionRequestDto
{
    public string Decision { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public decimal? SanctionAmount { get; set; }
    public decimal? InterestRate { get; set; }
}
