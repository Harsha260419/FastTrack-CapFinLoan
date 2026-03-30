namespace CapFinLoan.Admin.Application.DTOs;

public class ApplicationStatusResponseDto
{
    public Guid ApplicationId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
}
