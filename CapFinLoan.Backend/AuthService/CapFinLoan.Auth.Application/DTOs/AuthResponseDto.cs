namespace CapFinLoan.Auth.Application.DTOs;

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}