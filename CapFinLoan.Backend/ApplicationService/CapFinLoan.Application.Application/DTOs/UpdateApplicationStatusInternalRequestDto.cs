namespace CapFinLoan.Application.Application.DTOs;

public class UpdateApplicationStatusInternalRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}
