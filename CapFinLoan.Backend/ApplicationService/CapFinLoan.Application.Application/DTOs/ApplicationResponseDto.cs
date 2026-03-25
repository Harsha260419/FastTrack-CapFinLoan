namespace CapFinLoan.Application.Application.DTOs;

public class ApplicationResponseDto
{
    public Guid ApplicationId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public string LoanPurpose { get; set; } = string.Empty;
    public int TenureMonths { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? AdminRemarks { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
