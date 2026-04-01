namespace CapFinLoan.Auth.Application.DTOs;

public class SignupRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "APPLICANT";
}