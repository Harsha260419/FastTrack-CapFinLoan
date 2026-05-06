namespace CapFinLoan.Auth.Application.DTOs;

public class UpdatePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
