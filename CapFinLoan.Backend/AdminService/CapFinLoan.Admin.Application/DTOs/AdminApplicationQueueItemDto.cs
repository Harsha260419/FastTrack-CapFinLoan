namespace CapFinLoan.Admin.Application.DTOs;

public class AdminApplicationQueueItemDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
