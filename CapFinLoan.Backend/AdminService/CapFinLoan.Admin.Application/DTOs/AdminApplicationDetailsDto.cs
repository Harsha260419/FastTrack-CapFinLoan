namespace CapFinLoan.Admin.Application.DTOs;

public class AdminApplicationDetailsDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public string LoanPurpose { get; set; } = string.Empty;
    public int TenureMonths { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string DocumentVerificationStatus { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
