namespace CapFinLoan.Auth.Application.DTOs;

public class UpdateProfileRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
