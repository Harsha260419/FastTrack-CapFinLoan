namespace CapFinLoan.Admin.Application.DTOs;

public class UpdateApplicationStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}
