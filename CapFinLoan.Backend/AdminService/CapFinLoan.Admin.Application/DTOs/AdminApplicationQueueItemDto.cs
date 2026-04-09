namespace CapFinLoan.Admin.Application.DTOs;

public class AdminApplicationQueueItemDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public int TenureMonths { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
