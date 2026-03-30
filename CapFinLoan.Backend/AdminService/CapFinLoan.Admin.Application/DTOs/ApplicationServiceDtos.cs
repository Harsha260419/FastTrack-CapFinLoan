namespace CapFinLoan.Admin.Application.DTOs;

public class ApplicationServiceApplicationDto
{
    public Guid ApplicationId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public string LoanPurpose { get; set; } = string.Empty;
    public int TenureMonths { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
